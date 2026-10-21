using Lidgren.Network;
using SRMultiplayer.Packets;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static SRMultiplayer.Packets.PacketPlayerAnimation;

namespace SRMultiplayer.Networking
{
    public partial class NetworkPlayer : MonoBehaviour
    {
        float syncInterval = 0.1f;

        float m_AnimatorSpeed;
        float previousSpeed;

        // Note: not an object[] array because otherwise initialization is real annoying
        int[] lastIntParameters;
        float[] lastFloatParameters;
        bool[] lastBoolParameters;
        AnimatorControllerParameter[] parameters;

        // multiple layers
        int[] animationHash;
        int[] transitionHash;
        float[] layerWeight;
        float nextSendTime;

        IEnumerator SetupAnimator()
        {
            yield return new WaitForEndOfFrame();
            if (m_Animator == null || !m_Animator)
                yield break;
            // store the m_Animator parameters in a variable - the "Animator.parameters" getter allocates
            // a new parameter array every time it is accessed so we should avoid doing it in a loop
            parameters = m_Animator.parameters
                .Where(par => !m_Animator.IsParameterControlledByCurve(par.nameHash))
                .ToArray();
            if (parameters.Length > 64)
            {
                SRMP.Log($"[Animation] Animator for {Username} has {parameters.Length} network parameters; only the first 64 can be synchronized by this protocol.");
                parameters = parameters.Take(64).ToArray();
            }
            lastIntParameters = new int[parameters.Length];
            lastFloatParameters = new float[parameters.Length];
            lastBoolParameters = new bool[parameters.Length];

            animationHash = new int[m_Animator.layerCount];
            transitionHash = new int[m_Animator.layerCount];
            layerWeight = new float[m_Animator.layerCount];
            for (int i = 0; i < animationHash.Length; i++)
                layerWeight[i] = m_Animator.GetLayerWeight(i);
        }

        void FixedUpdate()
        {
            if (!Globals.GameLoaded || !IsLocal || !HasLoaded || m_Animator == null || !m_Animator.enabled || parameters == null)
                return;

            CheckSendRate();

            for (int i = 0; i < m_Animator.layerCount; i++)
            {
                int stateHash;
                float normalizedTime;
                if (!CheckAnimStateChanged(out stateHash, out normalizedTime, i))
                {
                    continue;
                }

                //NetOutgoingMessage writer = CreateMessage();

                //add the object to the writer but dont send it yet
                var packet = new PacketPlayerAnimation()
                {
                    Type = (byte)PacketPlayerAnimation.AnimationType.Layer,
                    ID = Globals.LocalID
                };

                //add extra parameters
                WriteAnimatorLayer(packet, stateHash, normalizedTime, i, layerWeight[i]);

                //send the changes
                packet.Send();
            }

            CheckSpeed();
        }

        void WriteAnimatorLayer(PacketPlayerAnimation writer, int stateHash, float normalizedTime, int layerNumber, float layerWeight)
        {
            writer.Add(stateHash);
            writer.Add(normalizedTime);
            writer.Add(layerNumber);
            writer.Add(layerWeight);
            WriteParameters(writer);
        }

        public void ReadAnimatorLayer(Queue<AnimationData> im)
        {
            if (m_Animator == null || !m_Animator || parameters == null || im == null || im.Count < 5) return;

            int stateHash = im.Dequeue().iData.Value;
            float normalizedTime = im.Dequeue().fData.Value;
            int layerNumber = im.Dequeue().iData.Value;
            float layerWeight = im.Dequeue().fData.Value;

            if (layerNumber < 0 || layerNumber >= m_Animator.layerCount) return;

            if (stateHash != 0 && m_Animator.enabled)
            {
                m_Animator.Play(stateHash, layerNumber, normalizedTime);
            }

            m_Animator.SetLayerWeight(layerNumber, layerWeight);

            ReadParameters(im);
        }

        void CheckSpeed()
        {
            float newSpeed = m_Animator.speed;
            if (Mathf.Abs(previousSpeed - newSpeed) > 0.001f)
            {
                previousSpeed = newSpeed;
                //NetOutgoingMessage writer = CreateMessage();

                //add the object to the writer but dont send it yet
                var packet = new PacketPlayerAnimation()
                {
                    Type = (byte)PacketPlayerAnimation.AnimationType.Speed,
                    ID = Globals.LocalID
                };

                //add extra parameters
                WriteAnimatorSpeed(packet, newSpeed);

                //send the speed change
                packet.Send();
            }
        }

        void WriteAnimatorSpeed(PacketPlayerAnimation writer, float newSpeed)
        {
            writer.Add(newSpeed);
        }

        public void ReadAnimatorSpeed(Queue<AnimationData> im)
        {
            if (m_Animator == null || !m_Animator || im == null || im.Count == 0) return;

            var newSpeed = im.Dequeue().fData.Value;
            // set m_Animator
            m_Animator.speed = newSpeed;
            m_AnimatorSpeed = newSpeed;
        }

        bool CheckAnimStateChanged(out int stateHash, out float normalizedTime, int layerId)
        {
            bool change = false;
            stateHash = 0;
            normalizedTime = 0;

            float lw = m_Animator.GetLayerWeight(layerId);
            if (Mathf.Abs(lw - layerWeight[layerId]) > 0.001f)
            {
                layerWeight[layerId] = lw;
                change = true;
            }

            if (m_Animator.IsInTransition(layerId))
            {
                AnimatorTransitionInfo tt = m_Animator.GetAnimatorTransitionInfo(layerId);
                if (tt.fullPathHash != transitionHash[layerId])
                {
                    // first time in this transition
                    transitionHash[layerId] = tt.fullPathHash;
                    animationHash[layerId] = 0;
                    return true;
                }
                return change;
            }

            AnimatorStateInfo st = m_Animator.GetCurrentAnimatorStateInfo(layerId);
            if (st.fullPathHash != animationHash[layerId])
            {
                // first time in this animation state
                // Include the initial state too. Leaving stateHash at zero on
                // the first update kept a newly joined player's model in T-pose
                // until its next animation transition.
                stateHash = st.fullPathHash;
                normalizedTime = st.normalizedTime;
                transitionHash[layerId] = 0;
                animationHash[layerId] = st.fullPathHash;
                return true;
            }
            return change;
        }

        void CheckSendRate()
        {
            float now = Time.time;
            if (IsLocal && syncInterval >= 0 && now > nextSendTime)
            {
                nextSendTime = now + syncInterval;

                //add the object to the writer but dont send it yet
                var packet = new PacketPlayerAnimation()
                {
                    Type = (byte)PacketPlayerAnimation.AnimationType.Parameters,
                    ID = Globals.LocalID
                };

                //add extra parameters
                if (WriteParameters(packet))
                {
                    packet.Send();
                }
            }
        }

        ulong NextDirtyBits()
        {
            ulong dirtyBits = 0;
            for (int i = 0; i < parameters.Length; i++)
            {
                AnimatorControllerParameter par = parameters[i];
                bool changed = false;
                if (par.type == AnimatorControllerParameterType.Int)
                {
                    int newIntValue = m_Animator.GetInteger(par.nameHash);
                    changed = newIntValue != lastIntParameters[i];
                    if (changed)
                        lastIntParameters[i] = newIntValue;
                }
                else if (par.type == AnimatorControllerParameterType.Float)
                {
                    float newFloatValue = m_Animator.GetFloat(par.nameHash);
                    changed = Mathf.Abs(newFloatValue - lastFloatParameters[i]) > 0.001f;
                    // only set lastValue if it was changed, otherwise value could slowly drift within the 0.001f limit each frame
                    if (changed)
                        lastFloatParameters[i] = newFloatValue;
                }
                else if (par.type == AnimatorControllerParameterType.Bool)
                {
                    bool newBoolValue = m_Animator.GetBool(par.nameHash);
                    changed = newBoolValue != lastBoolParameters[i];
                    if (changed)
                        lastBoolParameters[i] = newBoolValue;
                }
                if (changed)
                {
                    dirtyBits |= 1ul << i;
                }
            }
            return dirtyBits;
        }

        bool WriteParameters(PacketPlayerAnimation writer, bool forceAll = false)
        {
            ulong dirtyBits = forceAll ? (~0ul) : NextDirtyBits();
            writer.Add(dirtyBits);
            for (int i = 0; i < parameters.Length; i++)
            {
                if ((dirtyBits & (1ul << i)) == 0)
                    continue;

                AnimatorControllerParameter par = parameters[i];
                if (par.type == AnimatorControllerParameterType.Int)
                {
                    int newIntValue = m_Animator.GetInteger(par.nameHash);
                    writer.Add(newIntValue);
                }
                else if (par.type == AnimatorControllerParameterType.Float)
                {
                    float newFloatValue = m_Animator.GetFloat(par.nameHash);
                    writer.Add(newFloatValue);
                }
                else if (par.type == AnimatorControllerParameterType.Bool)
                {
                    bool newBoolValue = m_Animator.GetBool(par.nameHash);
                    writer.Add(newBoolValue);
                }
            }
            return dirtyBits != 0;
        }

        public void ReadParameters(Queue<AnimationData> im)
        {
            // SetupAnimator runs at end-of-frame after Spawn. Packets may arrive
            // during that window, so ignore them until the parameter table exists.
            if (m_Animator == null || !m_Animator || parameters == null || im == null || im.Count == 0) return;

            bool m_AnimatorEnabled = m_Animator.enabled;

            // need to read values from NetworkReader even if m_Animator is disabled
            var dirtyData = im.Dequeue();
            if (!dirtyData.uData.HasValue) return;
            ulong dirtyBits = dirtyData.uData.Value;
            int valueCount = 0;
            for (int i = 0; i < parameters.Length && i < 64; i++)
                if ((dirtyBits & (1ul << i)) != 0 && parameters[i].type != AnimatorControllerParameterType.Trigger)
                    valueCount++;
            if (im.Count < valueCount) return;

            for (int i = 0; i < parameters.Length; i++)
            {
                if ((dirtyBits & (1ul << i)) == 0)
                    continue;


                AnimatorControllerParameter par = parameters[i];

                if (par.type == AnimatorControllerParameterType.Int)
                {
                    int? newIntValue = im.Dequeue().iData;
                    if (m_AnimatorEnabled)
                        m_Animator.SetInteger(par.nameHash, newIntValue.Value);
                }
                else if (par.type == AnimatorControllerParameterType.Float)
                {
                    float? newFloatValue = im.Dequeue().fData;
                    if (m_AnimatorEnabled)
                        m_Animator.SetFloat(par.nameHash, newFloatValue.Value);
                }
                else if (par.type == AnimatorControllerParameterType.Bool)
                {
                    bool? newBoolValue = im.Dequeue().bData;
                    if (m_AnimatorEnabled)
                        m_Animator.SetBool(par.nameHash, newBoolValue.Value);
                }
            }
        }

    }
}
