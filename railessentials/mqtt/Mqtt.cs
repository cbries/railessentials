// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: Mqtt.cs

using System;
using System.Text;
using System.Threading.Tasks;
using ecoslib;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace railessentials.mqtt
{
    internal class Mqtt
    {
        private MqttClient _mqttClient;
        private string _mqttClientId;
        private readonly ConfiguratioMqtt _cfgMqtt;
        private readonly ILogger _logger;

        public Mqtt(ConfiguratioMqtt cfg, ILogger logger)
        {
            _cfgMqtt = cfg;
            _logger = logger;
        }

        public bool Init(out string resultMessage)
        {
            resultMessage = string.Empty;

            if (_cfgMqtt == null || !_cfgMqtt.Enabled)
            {
                resultMessage = "mqtt is not configured or enabled";
                return false;
            }

            if (_mqttClient is {IsConnected: true}) return true;

            try
            {
                _mqttClient = new MqttClient(_cfgMqtt.BrokerAddress);
                _mqttClientId = Guid.NewGuid().ToString();
                _mqttClient.Connect(_mqttClientId);
                return true;
            }
            catch (Exception ex)
            {
                _logger?.Log.Error($"Initialization of Mqtt failed: {ex.Message}");
                resultMessage = ex.Message;
            }

            return false;
        }

        public void Send(string topic, string state)
        {
            if (string.IsNullOrEmpty(topic)) return;
            if (state == null) return;

            Task.Run(() =>
            {
                var r = Init(out var resultMessage);
                if (!r)
                {
                    _logger?.Log.Error($"mqtt send failed: {resultMessage}");

                    return;
                }

                _mqttClient?.Publish(
                    topic, 
                    Encoding.UTF8.GetBytes(state), 
                    MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, 
                    true);
            });
        }
    }
}
