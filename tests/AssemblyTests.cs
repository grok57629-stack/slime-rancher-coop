using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Lidgren.Network;
using MonomiPark.SlimeRancher.DataModel;
using SRMultiplayer.Packets;
using SRMultiplayer.Packets.ModCompat;
using SRMultiplayer;
using SRMultiplayer.Networking;

internal static class AssemblyTests
{
    private static int checks;
    private static string root, managed;
    private static void Check(bool result, string message)
    {
        if (!result) throw new Exception(message);
        checks++;
        Console.WriteLine("PASS " + message);
    }

    private static int Main(string[] args)
    {
        root = Path.GetFullPath(args.Length > 0 ? args[0] : ".");
        managed = args.Length > 1 ? args[1] : @"C:\Program Files\Epic Games\SlimeRancher\SlimeRancher_Data\Managed";
        AppDomain.CurrentDomain.AssemblyResolve += Resolve;
        try { Run(); Console.WriteLine(checks + " assembly checks passed."); return 0; }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }

    private static Assembly Resolve(object sender, ResolveEventArgs args)
    {
        string name = new AssemblyName(args.Name).Name;
        foreach (string directory in new[] { Path.Combine(root, "Libs"), managed })
        {
            string file = Path.Combine(directory, name == "Assembly-CSharp" && directory.EndsWith("Libs") ? "Assembly-CSharp_publicized.dll" : name + ".dll");
            if (File.Exists(file)) return Assembly.LoadFrom(file);
        }
        return null;
    }

    private static T RoundTrip<T>(T packet) where T : Packet, new()
    {
        IPacket polymorphic = packet;
        var output = (NetOutgoingMessage)Activator.CreateInstance(typeof(NetOutgoingMessage), true);
        polymorphic.Serialize(output);
        var input = (NetIncomingMessage)Activator.CreateInstance(typeof(NetIncomingMessage), true);
        input.Data = output.Data;
        input.LengthBytes = output.LengthBytes;
        Check(input.ReadUInt16() == (ushort)packet.GetPacketType(), typeof(T).Name + " has one type header");
        T decoded = new T();
        ((IPacket)decoded).Deserialize(input);
        // Boolean packing may leave up to seven zero padding bits.
        Check(input.LengthBits - input.Position >= 0 && input.LengthBits - input.Position < 8, typeof(T).Name + " consumes its complete payload");
        return decoded;
    }

    private static void Run()
    {
        var owner = RoundTrip(new PacketActorOwner { ID = -123456, Owner = 254, Interaction = true });
        Check(owner.ID == -123456 && owner.Owner == 254 && owner.Interaction, "actor ownership intent round trip");
        var gordo = RoundTrip(new PacketGordoEat { ID = "gordo-test", Request = true, FoodID = 1234, Count = 50, Favorite = true,
            Position = new UnityEngine.Vector3(1, 2, 3), Rotation = new UnityEngine.Quaternion(0, 0, 0, 1) });
        Check(gordo.Request && gordo.FoodID == 1234 && gordo.Count == 50 && gordo.Position.z == 3 && gordo.Favorite, "Gordo request and absolute count round trip");
        var currency = RoundTrip(new PacketPlayerCurrency { Total = 321, Adjust = -45, Type = 0, Origin = 3 });
        Check(currency.Total == 321 && currency.Adjust == -45 && currency.Origin == 3, "currency acknowledgement round trip");
        var drone = RoundTrip(new PacketDroneAmmoAdd { ID = "site", Ident = 42, Count = 17, Overflow = true });
        Check(drone.Count == 17 && drone.Overflow && drone.Ident == 42, "drone add preserves full quantity");
        var remove = RoundTrip(new PacketDroneAmmoRemove { ID = "site", Count = 9 });
        Check(remove.Count == 9, "drone remove preserves full quantity");
        var spirit = RoundTrip(new PacketSpiritMode { Mode = PacketSpiritMode.BehaviorMode.Drain });
        Check(spirit.Mode == PacketSpiritMode.BehaviorMode.Drain, "custom serializer dispatches through IPacket");
        var actors = RoundTrip(new PacketActors { Actors = new List<PacketActors.ActorData>
        {
            new PacketActors.ActorData { ID = 123, Owner = 2, PlortDestroyTime = 9876.5 }
        } });
        Check(actors.Actors.Count == 1 && actors.Actors[0].PlortDestroyTime == 9876.5, "plort snapshot decodes without constructing a Unity GameObject");
        var actorPositions = RoundTrip(new PacketActorPositions { Positions = new List<PacketActorPositions.ActorPosition>
        {
            new PacketActorPositions.ActorPosition { ID = 123, Position = new UnityEngine.Vector3(1, 2, 3), Rotation = new UnityEngine.Quaternion(0, 0, 0, 1) },
            new PacketActorPositions.ActorPosition { ID = -456, Position = new UnityEngine.Vector3(4, 5, 6), Rotation = new UnityEngine.Quaternion(0, 1, 0, 0) }
        } });
        Check(actorPositions.Positions.Count == 2 && actorPositions.Positions[1].ID == -456
              && actorPositions.Positions[1].Position.z == 6, "batched actor transforms round trip");
        var oversizedPositions = new PacketActorPositions { Positions = Enumerable.Repeat(
            new PacketActorPositions.ActorPosition(), PacketActorPositions.MaxPositionsPerPacket + 1).ToList() };
        bool oversizedRejected = false;
        try
        {
            var output = (NetOutgoingMessage)Activator.CreateInstance(typeof(NetOutgoingMessage), true);
            oversizedPositions.Serialize(output);
        }
        catch (InvalidDataException) { oversizedRejected = true; }
        Check(oversizedRejected, "actor movement packet count is bounded before send");
        var silo = new AmmoModel { usableSlots = 2, slots = new Ammo.Slot[2] };
        silo.slots[1] = new Ammo.Slot(Identifiable.Id.PINK_SLIME, 3);
        silo.slots[1].emotions = new SlimeEmotionData();
        silo.slots[1].emotions.Add(SlimeEmotions.Emotion.HUNGER, 0.75f);
        var plotModel = new LandPlotModel { siloStorageIndices = Array.Empty<int>() };
        plotModel.siloAmmo.Add((SiloStorage.StorageType)0, silo);
        var landPlots = RoundTrip(new PacketLandplots
        {
            LandPlots = new List<PacketLandplots.LandPlotData>
            {
                new PacketLandplots.LandPlotData { ID = "test-silo", Model = plotModel }
            }
        });
        var decodedSilo = landPlots.LandPlots[0].Model.siloAmmo[(SiloStorage.StorageType)0];
        Check(decodedSilo.slots[0] == null && decodedSilo.slots[1].id == Identifiable.Id.PINK_SLIME
              && decodedSilo.slots[1].emotions.TryGetValue(SlimeEmotions.Emotion.HUNGER, out float hunger)
              && Math.Abs(hunger - 0.75f) < 0.001f,
            "silo emotion data stays on the correct nonzero ammo slot");
        var unsignedFashion = (Identifiable.Id)40000;
        var snareModel = new SnareModel((Gadget.Id)1, "test-snare", null)
        {
            fashions = new List<Identifiable.Id> { unsignedFashion }
        };
        var gadgetSnapshot = RoundTrip(new PacketGadgets
        {
            Gadgets = new List<PacketGadgets.GadgetSiteData>
            {
                new PacketGadgets.GadgetSiteData
                {
                    ID = "test-snare",
                    gadgetId = (Gadget.Id)1,
                    Model = snareModel
                }
            }
        });
        Check(((SnareModel)gadgetSnapshot.Gadgets[0].Model).fashions[0] == unsignedFashion,
            "gadget fashion IDs preserve their unsigned wire representation");
        var oversizedDronePrograms = (NetOutgoingMessage)Activator.CreateInstance(typeof(NetOutgoingMessage), true);
        oversizedDronePrograms.Write((ushort)PacketType.DronePrograms);
        oversizedDronePrograms.Write("test-drone");
        oversizedDronePrograms.Write(int.MaxValue);
        var malformedDronePrograms = (NetIncomingMessage)Activator.CreateInstance(typeof(NetIncomingMessage), true);
        malformedDronePrograms.Data = oversizedDronePrograms.Data;
        malformedDronePrograms.LengthBytes = oversizedDronePrograms.LengthBytes;
        malformedDronePrograms.ReadUInt16();
        bool malformedDroneProgramsRejected = false;
        try { new PacketDronePrograms(malformedDronePrograms); }
        catch (InvalidDataException) { malformedDroneProgramsRejected = true; }
        Check(malformedDroneProgramsRejected, "malformed drone program count is rejected before allocation");
        var animationPacket = new PacketPlayerAnimation
        {
            ID = 3,
            Type = (byte)PacketPlayerAnimation.AnimationType.Layer
        };
        animationPacket.Add(12345);
        animationPacket.Add(0.25f);
        animationPacket.Add(0);
        animationPacket.Add(1f);
        animationPacket.Add((ulong)0);
        var animationRoundTrip = RoundTrip(animationPacket);
        Check(animationRoundTrip.internalData.Count == 5, "player animation layer packet round trip");
        var relayCopy = animationPacket.CopyForRelay();
        animationPacket.internalData.Dequeue();
        Check(relayCopy.internalData.Count == 5 && relayCopy.internalData.Peek().iData == 12345,
            "animation relay retains an independent queue after local apply consumes the source");
        var oversizedAnimation = new PacketPlayerAnimation
        {
            ID = 3,
            Type = (byte)PacketPlayerAnimation.AnimationType.Parameters
        };
        for (int i = 0; i < PacketPlayerAnimation.MaxDataItems + 1; i++) oversizedAnimation.Add(i);
        bool oversizedAnimationRejected = false;
        try
        {
            var output = (NetOutgoingMessage)Activator.CreateInstance(typeof(NetOutgoingMessage), true);
            oversizedAnimation.Serialize(output);
        }
        catch (InvalidDataException) { oversizedAnimationRejected = true; }
        Check(oversizedAnimationRejected, "player animation packet size is bounded before send");
        CheckPacketScopes();

        Type authentication = typeof(Packet).Assembly.GetType("SRMultiplayer.EpicSDK.EpicAuthentication", true);
        MethodInfo statusChanged = authentication.GetMethod("OnConnectLoginStatusChanged", BindingFlags.Instance | BindingFlags.NonPublic);
        var notificationRemovals = PatchProcessor.GetOriginalInstructions(statusChanged)
            .Select(code => code.operand as MethodInfo)
            .Where(method => method != null && method.DeclaringType.FullName == "Epic.OnlineServices.Connect.ConnectInterface"
                             && method.Name.StartsWith("RemoveNotify", StringComparison.Ordinal))
            .Select(method => method.Name)
            .ToArray();
        Check(notificationRemovals.Count(name => name == "RemoveNotifyLoginStatusChanged") == 1
              && notificationRemovals.Count(name => name == "RemoveNotifyAuthExpiration") == 1,
            "EOS disconnect unregisters each callback with its matching API");

        // Use the actual compiled Harmony transpiler, then emit and execute its
        // replacement IL. The old one-argument helper produced an invalid stack.
        Type patch = typeof(Packet).Assembly.GetType("SRMultiplayer.Patches.PlayerState_Update", true);
        var transpiler = patch.GetMethod("Transpiler", BindingFlags.NonPublic | BindingFlags.Static);
        MethodInfo add = typeof(List<PlayerState.Upgrade>).GetMethod("Add");
        var method = new DynamicMethod("UpgradeRegression", typeof(void), new[] { typeof(List<PlayerState.Upgrade>), typeof(PlayerState.Upgrade) }, patch, true);
        ILGenerator il = method.GetILGenerator();
        var label = il.DefineLabel();
        var original = new CodeInstruction(OpCodes.Callvirt, add);
        original.labels.Add(label);
        var transformed = ((IEnumerable<CodeInstruction>)transpiler.Invoke(null, new object[] { new[] { original } })).Single();
        var helper = (MethodInfo)transformed.operand;
        Check(transformed.opcode == OpCodes.Call && helper.IsStatic && helper.GetParameters().Length == 2 && transformed.labels.Contains(label), "upgrade transpiler preserves stack signature and labels");
        il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Ldarg_1); il.Emit(transformed.opcode, helper); il.Emit(OpCodes.Ret);
        var call = (Action<List<PlayerState.Upgrade>, PlayerState.Upgrade>)method.CreateDelegate(typeof(Action<List<PlayerState.Upgrade>, PlayerState.Upgrade>));
        var upgrades = new List<PlayerState.Upgrade>();
        call(upgrades, (PlayerState.Upgrade)1);
        Check(upgrades.Count == 1 && (int)upgrades[0] == 1, "transformed upgrade IL executes and adds exactly once");

        Type gordoPatch = typeof(Packet).Assembly.GetType("SRMultiplayer.Patches.GordoEat_MaybeEat", true);
        var burstTranspiler = gordoPatch.GetMethod("Transpiler", BindingFlags.NonPublic | BindingFlags.Static);
        var startCoroutine = AccessTools.Method(typeof(UnityEngine.MonoBehaviour), "StartCoroutine", new[] { typeof(System.Collections.IEnumerator) });
        var burstInstructions = PatchProcessor.GetOriginalInstructions(typeof(GordoEat).GetMethod("MaybeEat"));
        Check(burstInstructions.Count(code => code.Calls(startCoroutine)) == 1, "Gordo original has one burst coroutine launch");
        var burstCode = ((IEnumerable<CodeInstruction>)burstTranspiler.Invoke(null, new object[] { burstInstructions })).ToList();
        Check(!burstCode.Any(code => code.Calls(startCoroutine)) && burstCode.Any(code => code.operand is MethodInfo m && m.Name == "StartBurst" && m.GetParameters().Length == 2), "Gordo coroutine launch is routed through the host scheduler");

        // These two wrappers are pure managed code. Unity native ECalls prevent
        // constructing the other wrappers in a normal .NET Framework process.
        var harmony = new Harmony("srmp.regression.patch-construction");
        foreach (string name in new[] { "PlayerState_Update", "Ammo_MaybeAddToSlot" })
        {
            Type changedPatch = typeof(Packet).Assembly.GetType("SRMultiplayer.Patches." + name, true);
            var patched = harmony.CreateClassProcessor(changedPatch).Patch();
            Check(patched != null && patched.Count > 0, "Harmony wrapper construction: " + name);
        }
        harmony.UnpatchAll(harmony.Id);

        // Validate target resolution and named injections for the Unity patches
        // without claiming that their native simulation has been executed.
        foreach (string name in new[] {
            "GameModel_RegisterStartingActor", "Identifiable_IsEdible",
            "GordoEat_MaybeEat", "GordoEat_ImmediateReachedTarget", "SlimeEat_MaybeChomp",
            "SlimeEat_MaybeSpinAndChomp", "SlimeEat_FinishChomp", "SlimeFeral_RegistryUpdate",
            "SlimeFeral_MakeFeral", "PlortCollector_Update", "PlortCollector_StartCollection",
            "PlortCollector_DoCollection", "Nutcracker_DoCrack", "DroneAnimator_SetAnimation",
            "DirectedActorSpawner_CanSpawn", "DirectedActorSpawner_CanContinueSpawning"
        })
        {
            Type changedPatch = typeof(Packet).Assembly.GetType("SRMultiplayer.Patches." + name, true);
            var info = HarmonyMethod.Merge(changedPatch.GetCustomAttributes(true).OfType<HarmonyPatch>().Select(a => a.info).ToList());
            MethodInfo target = AccessTools.DeclaredMethod(info.declaringType, info.methodName, info.argumentTypes);
            Check(target != null, "Harmony target resolves: " + name);
            foreach (var hook in changedPatch.GetMethods(BindingFlags.Static | BindingFlags.NonPublic).Where(m => m.Name == "Prefix" || m.Name == "Postfix"))
            foreach (var argument in hook.GetParameters())
            {
                Type actual = argument.ParameterType.IsByRef ? argument.ParameterType.GetElementType() : argument.ParameterType;
                if (argument.Name == "__state") continue;
                Type expected = argument.Name == "__instance" ? target.DeclaringType : argument.Name == "__result" ? target.ReturnType :
                    target.GetParameters().Single(p => p.Name == argument.Name).ParameterType;
                if (actual != expected) throw new Exception(name + ": invalid injection " + argument.Name);
            }
            Check(true, "Harmony injection signatures: " + name);
        }

        // Unity destroyed objects compare equal to null, so the guard can be
        // exercised without initializing Unity's native runtime by passing a
        // literal null. A stale FindConsumable entry must be treated as inedible.
        Type ediblePatch = typeof(Packet).Assembly.GetType("SRMultiplayer.Patches.Identifiable_IsEdible", true);
        MethodInfo ediblePrefix = ediblePatch.GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
        object[] edibleArguments = { null, true };
        bool continueOriginal = (bool)ediblePrefix.Invoke(null, edibleArguments);
        Check(!continueOriginal && !(bool)edibleArguments[1], "stale consumable guard skips null/destroyed Unity objects");

        MethodInfo destroyActor = typeof(NetworkActor).GetMethod("DestroyRegistered", BindingFlags.NonPublic | BindingFlags.Static);
        Check(destroyActor != null && destroyActor.ReturnType == typeof(void), "network actor destroy has a central guarded cleanup entry point");
        foreach (Type handlerType in new[] { typeof(NetworkHandlerClient), typeof(NetworkHandlerServer) })
        {
            MethodInfo handler = handlerType.GetMethod("OnActorDestroy", BindingFlags.NonPublic | BindingFlags.Static);
            var instructions = PatchProcessor.GetOriginalInstructions(handler);
            Check(instructions.Any(code => code.operand is MethodInfo called && called == destroyActor),
                handlerType.Name + " routes actor despawns through guarded cleanup");
        }
    }

    public class ScopeProbe : Packet
    {
        public static Action Action;
        public ScopeProbe(NetIncomingMessage im) { }
        public void HandleClient(ScopeProbe packet) { Action(); }
        public void HandleServer(ScopeProbe packet, NetworkPlayer player) { Action(); }
    }

    private static void CheckPacketScopes()
    {
        var registry = (Dictionary<PacketType, Type>)typeof(Globals).GetField("CustomPackets", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
        var type = (PacketType)65000;
        registry.Add(type, typeof(ScopeProbe));
        try
        {
            foreach (bool server in new[] { false, true })
            {
                Action dispatch = () =>
                {
                    var input = (NetIncomingMessage)Activator.CreateInstance(typeof(NetIncomingMessage), true);
                    if (server) NetworkHandlerServer.HandlePacket(type, input, null);
                    else NetworkHandlerClient.HandlePacket(type, input);
                };
                foreach (bool prior in new[] { false, true })
                {
                    Globals.HandlePacket = prior;
                    ScopeProbe.Action = () => { if (!Globals.HandlePacket) throw new Exception("missing packet scope"); };
                    dispatch();
                    Check(Globals.HandlePacket == prior, "custom packet early return restores scope " + server + "/" + prior);
                    ScopeProbe.Action = () => { throw new InvalidDataException("simulated packet failure"); };
                    bool threw = false;
                    try { dispatch(); } catch (Exception) { threw = true; }
                    Check(!threw && Globals.HandlePacket == prior, "packet exception is logged and contained; scope restored " + server + "/" + prior);
                }
                Globals.HandlePacket = false;
                ScopeProbe.Action = () =>
                {
                    ScopeProbe.Action = () => { };
                    dispatch();
                    if (!Globals.HandlePacket) throw new Exception("nested handler cleared outer scope");
                };
                dispatch();
                Check(!Globals.HandlePacket, "nested packet handling restores both scopes " + server);
            }
        }
        finally { registry.Remove(type); ScopeProbe.Action = null; Globals.HandlePacket = false; }
    }
}
