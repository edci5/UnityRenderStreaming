using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Unity.RenderStreaming
{
    public class Broadcast : SignalingHandlerBase,
        IOfferHandler, IAddChannelHandler, IDisconnectHandler
    {
        [SerializeField]
        private List<Component> streams = new List<Component>();

        private List<string> connectionIds = new List<string>();

        public void AddComponent(Component component)
        {
            streams.Add(component);
        }

        public void RemoveComponent(Component component)
        {
            streams.Remove(component);
        }

        public void OnDisconnect(SignalingEventData data)
        {
            if (!connectionIds.Contains(data.connectionId))
                return;
            connectionIds.Remove(data.connectionId);

            foreach (var source in streams.OfType<IStreamSource>())
            {
                source.SetSender(data.connectionId, null);
            }
            foreach (var receiver in streams.OfType<IStreamReceiver>())
            {
                receiver.SetReceiver(data.connectionId, null);
            }
            foreach (var channel in streams.OfType<IDataChannel>())
            {
                channel.SetChannel(data.connectionId, null);
            }
        }

        public void OnOffer(SignalingEventData data)
        {
            if (connectionIds.Contains(data.connectionId))
            {
                Debug.Log($"Already answered this connectionId : {data.connectionId}");
                return;
            }
            connectionIds.Add(data.connectionId);

            foreach (var source in streams.OfType<IStreamSource>())
            {
                var transceiver = AddTrack(data.connectionId, source.Track);
                source.SetSender(data.connectionId, transceiver.Sender);
            }
            foreach (var channel in streams.OfType<IDataChannel>().Where(c => c.IsLocal))
            {
                var _channel = CreateChannel(data.connectionId, channel.Label);
                channel.SetChannel(data.connectionId, _channel);
            }
            SendAnswer(data.connectionId);
        }

        public void OnAddChannel(SignalingEventData data)
        {
            var channel = streams.OfType<IDataChannel>().
                FirstOrDefault(r => r.Channel == null && !r.IsLocal);
            channel?.SetChannel(data.connectionId, data.channel);
        }
    }
}
