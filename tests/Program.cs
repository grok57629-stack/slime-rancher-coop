using System;
using System.IO;
using System.Linq;
using SRMultiplayer.Networking;
using SRMultiplayer;

internal static class Program
{
    private static int checks;
    private static void Check(bool value, string name)
    {
        if (!value) throw new Exception(name);
        checks++;
        Console.WriteLine("PASS " + name);
    }

    private static byte[] Payload(int length, int seed)
    {
        var data = new byte[length];
        new Random(seed).NextBytes(data);
        return data;
    }

    private static void Reject(Action action, string name)
    {
        try { action(); }
        catch (InvalidDataException) { Check(true, name); return; }
        throw new Exception("Accepted invalid input: " + name);
    }

    private static void Main()
    {
        string code = ServerCode.Generate();
        Check(code.Length == ServerCode.Length && ServerCode.IsValid(code), "generated server codes match the advertised format");
        Check(ServerCode.Normalize(" fuy-wvni ") == "FUYWVNI", "server code normalization supports pasted separators and case");
        Check(ServerCode.IsValid(ServerCode.Normalize("fuywvni")), "seven-character server code is accepted");
        Check(!ServerCode.IsValid(ServerCode.Normalize("fuywv")), "five-character server code is rejected");
        Check(!ServerCode.IsValid(ServerCode.Normalize("fuywvni!")), "invalid server code characters are rejected");
        Check(!ServerCode.IsValid(ServerCode.Normalize("fuywvnix")), "overlong server code is rejected");

        Check(NetworkIdentity.IsSameProductUser("0002854b61554b77a3173e515033a366", "0002854b61554b77a3173e515033a366"), "matching EOS product IDs identify a self-join");
        Check(!NetworkIdentity.IsSameProductUser("0002854b61554b77a3173e515033a366", "00028248ab684b6492410312a6fa045c"), "different EOS product IDs remain distinct");
        Check(!NetworkIdentity.IsSameProductUser("", "0002854b61554b77a3173e515033a366"), "missing EOS identity is not treated as a self-join");

        Check(RegionOwnership.SelectReplacementOwner(1, 2, new byte[] { 2, 3, 1 }) == 1, "region handoff prefers a host present in the region");
        Check(RegionOwnership.SelectReplacementOwner(1, 2, new byte[] { 2, 4, 3 }) == 3, "region handoff chooses a connected remote when host is absent");
        Check(RegionOwnership.SelectReplacementOwner(1, 2, new byte[] { 2, 0 }) == 0, "region with no remaining players becomes claimable");

        // Includes the old byte-counter overflow and capacity-vs-length bug.
        foreach (int length in new[] { 1, 999, 1000, 1001, 255000, 256001, 1048577 })
        {
            var original = Payload(length + 64, length);
            var frames = PacketFraming.Encode(17, 1, original, length).ToArray();
            var receiver = new PacketReassembler();
            byte[] result = null;
            foreach (var frame in frames.Reverse())
                if (receiver.TryReceive(frame, frame.Length, 0, out ushort type, out var data))
                    result = data;
            Check(result != null && result.SequenceEqual(original.Take(length)), "round trip " + length);
            Check(receiver.PendingCount == 0, "completed message released " + length);
        }

        var a = Payload(3010, 1);
        var b = Payload(3010, 2);
        var af = PacketFraming.Encode(9, 41, a, a.Length).ToArray();
        var bf = PacketFraming.Encode(9, 42, b, b.Length).ToArray();
        var multiplexed = new PacketReassembler();
        byte[] ar = null, br = null;
        for (int i = 0; i < af.Length; i++)
        {
            if (multiplexed.TryReceive(af[i], af[i].Length, 0, out _, out var data)) ar = data;
            Check(!multiplexed.TryReceive(af[i], af[i].Length, 0, out _, out _), "duplicate fragment ignored " + i);
            if (multiplexed.TryReceive(bf[i], bf[i].Length, 0, out _, out data)) br = data;
        }
        Check(ar.SequenceEqual(a) && br.SequenceEqual(b), "interleaved messages of the same type");

        // Both peers start their counter at the same id, as on a real join.
        bf = PacketFraming.Encode(9, 41, b, b.Length).ToArray();
        var peerA = new PacketReassembler();
        var peerB = new PacketReassembler();
        for (int i = 0; i < af.Length; i++)
        {
            if (peerA.TryReceive(af[i], af[i].Length, 0, out _, out var data)) ar = data;
            if (peerB.TryReceive(bf[i], bf[i].Length, 0, out _, out data)) br = data;
        }
        Check(ar.SequenceEqual(a) && br.SequenceEqual(b), "interleaved peers with identical message ids");

        var lost = new PacketReassembler();
        lost.TryReceive(af[0], af[0].Length, 0, out _, out _);
        lost.Expire(31);
        Check(lost.PendingCount == 0, "lost fragment expires without leaking a buffer");
        var fresh = PacketFraming.Encode(9, 43, b, b.Length).ToArray();
        br = null;
        foreach (var frame in fresh)
            if (lost.TryReceive(frame, frame.Length, 32, out _, out var data)) br = data;
        Check(br.SequenceEqual(b), "loss does not corrupt the next message");

        Reject(() => lost.TryReceive(af[0], 3, 0, out _, out _), "truncated header");
        Reject(() => lost.TryReceive(af[0], af[0].Length - 1, 0, out _, out _), "truncated payload");
        var bad = (byte[])af[0].Clone();
        bad[18] = 0; bad[19] = 0;
        Reject(() => lost.TryReceive(bad, bad.Length, 0, out _, out _), "zero fragments");
        bad = (byte[])af[0].Clone(); bad[16] = 250;
        Reject(() => lost.TryReceive(bad, bad.Length, 0, out _, out _), "index out of range");
        bad = (byte[])af[0].Clone(); bad[4] = 255;
        Reject(() => lost.TryReceive(bad, bad.Length, 0, out _, out _), "protocol mismatch");
        bad = (byte[])af[0].Clone(); bad[4] = 2; bad[5] = 0;
        Reject(() => lost.TryReceive(bad, bad.Length, 0, out _, out _), "legacy protocol version 2 is rejected");
        var conflict = new PacketReassembler();
        conflict.TryReceive(af[0], af[0].Length, 0, out _, out _);
        bad = PacketFraming.Encode(10, 41, a, a.Length).First();
        Reject(() => conflict.TryReceive(bad, bad.Length, 0, out _, out _), "conflicting message type");
        var rejoined = new PacketReassembler();
        ar = null;
        foreach (var frame in af)
            if (rejoined.TryReceive(frame, frame.Length, 0, out _, out var data)) ar = data;
        Check(ar.SequenceEqual(a), "new session may reuse message ids");
        var bounded = new PacketReassembler();
        for (uint id = 1; id <= 32; id++)
        {
            var frame = PacketFraming.Encode(7, id, a, a.Length).First();
            bounded.TryReceive(frame, frame.Length, 0, out _, out _);
        }
        var overflow = PacketFraming.Encode(7, 33, a, a.Length).First();
        Reject(() => bounded.TryReceive(overflow, overflow.Length, 0, out _, out _), "incomplete message count is bounded");
        bounded.Expire(31);
        Check(bounded.PendingCount == 0, "all incomplete allocations released on expiry");

        Check(NetworkIdentity.ForScenePath("hello") == 1335831723, "specified scene hash test vector");
        var ids = new System.Collections.Generic.HashSet<int>();
        for (int peer = 1; peer <= 255; peer++)
            for (int sequence = 1; sequence <= 100; sequence++) ids.Add(NetworkIdentity.ForActor((byte)peer, sequence));
        Check(ids.Count == 25500 && !ids.Contains(0), "255 actor namespaces are disjoint including signed ids");
        Check(NetworkIdentity.ForActor(2, 0xFFFFFF) != NetworkIdentity.ForActor(3, 1), "actor namespace boundary");
        bool rejected = false;
        try { NetworkIdentity.ForActor(1, 0x1000000); }
        catch (ArgumentOutOfRangeException) { rejected = true; }
        Check(rejected, "actor sequence cannot wrap into another namespace");
        var host = new Guid("01234567-89ab-cdef-0123-456789abcdef");
        string name = NetworkIdentity.InventoryFile(host, "world");
        Check(name == NetworkIdentity.InventoryFile(host, "world"), "inventory identity survives reconnect");
        Check(name != NetworkIdentity.InventoryFile(Guid.NewGuid(), "world"), "same world name on different hosts is isolated");
        Check(name != NetworkIdentity.InventoryFile(host, "world2"), "different worlds on the same host are isolated");
        Check(Path.GetFileName(NetworkIdentity.InventoryFile(host, "../world:name")) == NetworkIdentity.InventoryFile(host, "../world:name"), "world name cannot escape inventory directory");
        Guid saveId = NetworkIdentity.ForPlayerSave("authenticated-product-user-a");
        Check(saveId == NetworkIdentity.ForPlayerSave("authenticated-product-user-a"), "player save identity survives reconnect");
        Check(saveId != NetworkIdentity.ForPlayerSave("authenticated-product-user-b"), "players cannot select the same save identity");
        Check(saveId != Guid.Empty, "player save identity is nonempty");

        // Only this unique test directory is ever cleaned up.
        string directory = Path.Combine(Path.GetTempPath(), "SRMP-regression-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            string save = Path.Combine(directory, "player.save");
            AtomicFile.WriteText(save, "first");
            Check(File.ReadAllText(save) == "first", "atomic first save");
            AtomicFile.WriteText(save, "second");
            Check(File.ReadAllText(save) == "second" && File.ReadAllText(save + ".bak") == "first", "atomic replacement preserves backup");
            try { AtomicFile.Write(save, stream => { stream.WriteByte(42); throw new IOException("simulated interrupted write"); }); }
            catch (IOException) { }
            Check(File.ReadAllText(save) == "second" && File.ReadAllText(save + ".bak") == "first", "interrupted save retains both previous files");
            Check(Directory.GetFiles(directory, "*.tmp").Length == 0, "failed save cleans only temporary output");
            AtomicFile.WriteText(save, "third");
            Check(File.ReadAllText(save) == "third" && File.ReadAllText(save + ".bak") == "second", "saving recovers after an interrupted write");
        }
        finally { foreach (string file in Directory.GetFiles(directory)) File.Delete(file); Directory.Delete(directory); }
        Console.WriteLine(checks + " checks passed.");
    }
}
