﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NeighborDiscovery.Protocols;
using NeighborDiscovery.Statistics;
using NeighborDiscovery.Utils;

namespace NeighborDiscovery.Environment
{
    public sealed class NeighborDiscoveryEnvironment
    {
        private NodeType _protocolType;
        private int _accId;

        private readonly Random _random = new Random();

        private int EndsAt(int startUpSlot, BoundedProtocol device)
        {
            return startUpSlot + device.Bound;
        }
        
        private DiscoverableDevice FromDeviceDataToDiscoverableDevice(DeviceData data)
        {
            BoundedProtocol logic;
            if (data.Id == _accId)
            {
                switch (_protocolType)
                {
                    case NodeType.BalancedNihao:
                        logic = CreateProtocol(data.Id, data.DutyCycle, NodeType.AccGreedyBalancedNihao);
                        break;

                    default:
                        logic = CreateProtocol(data.Id, data.DutyCycle, _protocolType);
                        break;
                }
            }
            else 
                logic = CreateProtocol(data.Id, data.DutyCycle, _protocolType);
            logic.MoveNext(_random.Next(logic.T));//todo: change this :)
            return new DiscoverableDevice(logic, data.Position, data.Direction, data.Speed, data.CommunicationRange);
        }

        private Event CreateIncomingEvent(DeviceData data)
        {
            return new Event(FromDeviceDataToDiscoverableDevice(data), EventType.IncomingDevice);
        }

        public BoundedProtocol CreateProtocol(int id, int dutyCycle, NodeType nodeType)
        {
            switch (nodeType)
            {
                case NodeType.Birthday:
                    return null;
                case NodeType.Disco:
                    return new Disco(id, dutyCycle);
                case NodeType.UConnect:
                    return new UConnect(id, dutyCycle);
                case NodeType.Searchlight:
                    return null;
                case NodeType.StripedSearchlight:
                    return null;
                case NodeType.Hello:
                    return null;
                case NodeType.TestAlgorithm:
                    return null;
                case NodeType.GNihao:
                    return null;
                case NodeType.BalancedNihao:
                    return new BalancedNihaoTmll(id, dutyCycle);
                case NodeType.AccGossipGNihao:
                    return null;
                case NodeType.AccGossipPNihao:
                    return null;
                case NodeType.AccGreedyBalancedNihao:
                    return new AccBalancedNihaoGreedy(id, dutyCycle);
                default:
                {
                    throw new ArgumentException(_protocolType.ToString() + "(protocol) not supported");
                }
            }
        }

        public StatisticTestResult RunSingleSimulation(IEnumerable<DeviceData> data, NodeType protocolType, int accId = -1)
        {
            _accId = accId;
            _protocolType = protocolType;
            List<DeviceData> dataList = data.ToList();
            dataList.Sort();
            Queue<DeviceData> events = new Queue<DeviceData>(dataList);
            int maxSlot = dataList[dataList.Count - 1].StartUpSlot * 2  + 1;//todo, improve the way to calculate the limit
            //int maxSlot = 1000;
            int currentSlot = 0;
            FullDiscoveryEnvironmentTmll fullEnv = new FullDiscoveryEnvironmentTmll(RunningMode.StaticDevices);
            while (currentSlot < maxSlot)
            {
                while (events.Count > 0 && events.Peek().StartUpSlot == currentSlot)
                {
                    fullEnv.AddEvent(CreateIncomingEvent(events.Dequeue()));
                }
                fullEnv.MoveNext();
                currentSlot++;
            }
            if(_accId >= 0)
                return fullEnv.GetCurrentResult(_accId);
            return fullEnv.GetCurrentResult();
        }
    }
}
