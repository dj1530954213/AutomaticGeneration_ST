using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinFormsApp1.Devices.Base;
using WinFormsApp1.Devices.Interfaces;
using AutomaticGeneration_ST.Models;
using System.Drawing;
using Newtonsoft.Json;
using System.Collections.Concurrent;

namespace WinFormsApp1.Devices
{
    /// <summary>
    /// 设备拓扑管理器 - 管理设备间的拓扑关系、布局和可视化
    /// </summary>
    public class DeviceTopologyManager
    {
        #region 拓扑类型枚举

        /// <summary>
        /// 拓扑类型
        /// </summary>
        public enum TopologyType
        {
            /// <summary>
            /// 树形拓扑
            /// </summary>
            Tree,
            
            /// <summary>
            /// 网状拓扑
            /// </summary>
            Mesh,
            
            /// <summary>
            /// 星形拓扑
            /// </summary>
            Star,
            
            /// <summary>
            /// 环形拓扑
            /// </summary>
            Ring,
            
            /// <summary>
            /// 总线拓扑
            /// </summary>
            Bus,
            
            /// <summary>
            /// 分层拓扑
            /// </summary>
            Hierarchical
        }

        /// <summary>
        /// 节点类型
        /// </summary>
        public enum NodeType
        {
            /// <summary>
            /// 设备节点
            /// </summary>
            Device,
            
            /// <summary>
            /// 连接节点
            /// </summary>
            Junction,
            
            /// <summary>
            /// 网关节点
            /// </summary>
            Gateway,
            
            /// <summary>
            /// 控制器节点
            /// </summary>
            Controller,
            
            /// <summary>
            /// 传感器节点
            /// </summary>
            Sensor,
            
            /// <summary>
            /// 执行器节点
            /// </summary>
            Actuator
        }

        /// <summary>
        /// 连接类型
        /// </summary>
        public enum ConnectionType
        {
            /// <summary>
            /// 物理连接
            /// </summary>
            Physical,
            
            /// <summary>
            /// 逻辑连接
            /// </summary>
            Logical,
            
            /// <summary>
            /// 通讯连接
            /// </summary>
            Communication,
            
            /// <summary>
            /// 控制连接
            /// </summary>
            Control,
            
            /// <summary>
            /// 数据连接
            /// </summary>
            Data,
            
            /// <summary>
            /// 电源连接
            /// </summary>
            Power
        }

        #endregion

        #region 内部类定义

        /// <summary>
        /// 拓扑节点
        /// </summary>
        public class TopologyNode
        {
            public string NodeId { get; set; } = "";
            public string NodeName { get; set; } = "";
            public NodeType NodeType { get; set; }
            public string DeviceId { get; set; } = "";
            public PointF Position { get; set; }
            public SizeF Size { get; set; } = new SizeF(60, 40);
            public Color NodeColor { get; set; } = Color.LightBlue;
            public string Description { get; set; } = "";
            public Dictionary<string, object> Properties { get; set; } = new();
            public bool IsVisible { get; set; } = true;
            public bool IsSelectable { get; set; } = true;
            public int Layer { get; set; } = 0;
        }

        /// <summary>
        /// 拓扑连接
        /// </summary>
        public class TopologyConnection
        {
            public string ConnectionId { get; set; } = "";
            public string SourceNodeId { get; set; } = "";
            public string TargetNodeId { get; set; } = "";
            public ConnectionType ConnectionType { get; set; }
            public string ConnectionName { get; set; } = "";
            public Color ConnectionColor { get; set; } = Color.Black;
            public float LineWidth { get; set; } = 1.0f;
            public bool IsDirectional { get; set; } = true;
            public List<PointF> ControlPoints { get; set; } = new();
            public Dictionary<string, object> Properties { get; set; } = new();
            public bool IsVisible { get; set; } = true;
        }

        /// <summary>
        /// 拓扑图
        /// </summary>
        public class TopologyDiagram
        {
            public string DiagramId { get; set; } = "";
            public string DiagramName { get; set; } = "";
            public TopologyType TopologyType { get; set; }
            public Dictionary<string, TopologyNode> Nodes { get; set; } = new();
            public Dictionary<string, TopologyConnection> Connections { get; set; } = new();
            public RectangleF ViewBounds { get; set; } = new RectangleF(0, 0, 1000, 800);
            public float ZoomLevel { get; set; } = 1.0f;
            public PointF ViewCenter { get; set; } = new PointF(500, 400);
            public DateTime LastModified { get; set; } = DateTime.Now;
        }

        /// <summary>
        /// 布局算法接口
        /// </summary>
        public interface ILayoutAlgorithm
        {
            void ApplyLayout(TopologyDiagram diagram);
            string AlgorithmName { get; }
        }

        /// <summary>
        /// 层次布局算法
        /// </summary>
        public class HierarchicalLayout : ILayoutAlgorithm
        {
            public string AlgorithmName => "分层布局";

            public void ApplyLayout(TopologyDiagram diagram)
            {
                var nodes = diagram.Nodes.Values.ToList();
                var layers = new Dictionary<int, List<TopologyNode>>();

                // 计算节点层级
                foreach (var node in nodes)
                {
                    var layer = CalculateNodeLayer(node, diagram);
                    if (!layers.ContainsKey(layer))
                        layers[layer] = new List<TopologyNode>();
                    layers[layer].Add(node);
                }

                // 按层布局
                float yOffset = 50;
                float layerHeight = 120;

                foreach (var layer in layers.OrderBy(l => l.Key))
                {
                    var layerNodes = layer.Value;
                    float xOffset = 100;
                    float nodeSpacing = Math.Max(150, diagram.ViewBounds.Width / (layerNodes.Count + 1));

                    for (int i = 0; i < layerNodes.Count; i++)
                    {
                        layerNodes[i].Position = new PointF(xOffset + i * nodeSpacing, yOffset);
                        layerNodes[i].Layer = layer.Key;
                    }

                    yOffset += layerHeight;
                }
            }

            private int CalculateNodeLayer(TopologyNode node, TopologyDiagram diagram)
            {
                // 计算节点在层次结构中的层级
                var incomingConnections = diagram.Connections.Values
                    .Where(c => c.TargetNodeId == node.NodeId).ToList();

                if (!incomingConnections.Any())
                    return 0; // 根节点

                var maxParentLayer = incomingConnections
                    .Select(c => diagram.Nodes.GetValueOrDefault(c.SourceNodeId))
                    .Where(n => n != null)
                    .Max(n => n!.Layer);

                return maxParentLayer + 1;
            }
        }

        /// <summary>
        /// 力导向布局算法
        /// </summary>
        public class ForceDirectedLayout : ILayoutAlgorithm
        {
            public string AlgorithmName => "力导向布局";

            private const float RepulsionForce = 1000.0f;
            private const float AttractionForce = 0.1f;
            private const float Damping = 0.9f;
            private const int MaxIterations = 100;

            public void ApplyLayout(TopologyDiagram diagram)
            {
                var nodes = diagram.Nodes.Values.ToList();
                var velocities = new Dictionary<string, PointF>();

                // 初始化速度
                foreach (var node in nodes)
                {
                    velocities[node.NodeId] = new PointF(0, 0);
                }

                // 迭代计算力的影响
                for (int iteration = 0; iteration < MaxIterations; iteration++)
                {
                    var forces = new Dictionary<string, PointF>();

                    // 初始化力
                    foreach (var node in nodes)
                    {
                        forces[node.NodeId] = new PointF(0, 0);
                    }

                    // 计算排斥力
                    for (int i = 0; i < nodes.Count; i++)
                    {
                        for (int j = i + 1; j < nodes.Count; j++)
                        {
                            var node1 = nodes[i];
                            var node2 = nodes[j];

                            var dx = node2.Position.X - node1.Position.X;
                            var dy = node2.Position.Y - node1.Position.Y;
                            var distance = Math.Max(1.0f, (float)Math.Sqrt(dx * dx + dy * dy));

                            var force = RepulsionForce / (distance * distance);
                            var fx = (float)(force * dx / distance);
                            var fy = (float)(force * dy / distance);

                            forces[node1.NodeId] = new PointF(
                                forces[node1.NodeId].X - fx,
                                forces[node1.NodeId].Y - fy);
                            forces[node2.NodeId] = new PointF(
                                forces[node2.NodeId].X + fx,
                                forces[node2.NodeId].Y + fy);
                        }
                    }

                    // 计算吸引力（连接的节点间）
                    foreach (var connection in diagram.Connections.Values)
                    {
                        var sourceNode = diagram.Nodes.GetValueOrDefault(connection.SourceNodeId);
                        var targetNode = diagram.Nodes.GetValueOrDefault(connection.TargetNodeId);

                        if (sourceNode != null && targetNode != null)
                        {
                            var dx = targetNode.Position.X - sourceNode.Position.X;
                            var dy = targetNode.Position.Y - sourceNode.Position.Y;
                            var distance = Math.Max(1.0f, (float)Math.Sqrt(dx * dx + dy * dy));

                            var force = AttractionForce * distance;
                            var fx = (float)(force * dx / distance);
                            var fy = (float)(force * dy / distance);

                            forces[sourceNode.NodeId] = new PointF(
                                forces[sourceNode.NodeId].X + fx,
                                forces[sourceNode.NodeId].Y + fy);
                            forces[targetNode.NodeId] = new PointF(
                                forces[targetNode.NodeId].X - fx,
                                forces[targetNode.NodeId].Y - fy);
                        }
                    }

                    // 更新位置
                    foreach (var node in nodes)
                    {
                        var velocity = velocities[node.NodeId];
                        var force = forces[node.NodeId];

                        velocity = new PointF(
                            (velocity.X + force.X) * Damping,
                            (velocity.Y + force.Y) * Damping);

                        velocities[node.NodeId] = velocity;

                        node.Position = new PointF(
                            Math.Max(50, Math.Min(diagram.ViewBounds.Width - 50, node.Position.X + velocity.X)),
                            Math.Max(50, Math.Min(diagram.ViewBounds.Height - 50, node.Position.Y + velocity.Y)));
                    }
                }
            }
        }

        #endregion

        #region 私有字段

        private readonly ConcurrentDictionary<string, TopologyDiagram> _diagrams = new();
        private readonly Dictionary<string, ILayoutAlgorithm> _layoutAlgorithms = new();
        private readonly CompositeDeviceManager _deviceManager;
        private string _currentDiagramId = "";

        #endregion

        #region 事件定义

        /// <summary>
        /// 拓扑变更事件参数
        /// </summary>
        public class TopologyChangedEventArgs : EventArgs
        {
            public string DiagramId { get; set; } = "";
            public string ChangeType { get; set; } = "";
            public string AffectedNodeId { get; set; } = "";
            public DateTime Timestamp { get; set; } = DateTime.Now;
        }

        #endregion

        #region 事件

        /// <summary>
        /// 拓扑变更事件
        /// </summary>
        public event EventHandler<TopologyChangedEventArgs>? TopologyChanged;

        /// <summary>
        /// 节点选择事件
        /// </summary>
        public event EventHandler<string>? NodeSelected;

        #endregion

        #region 构造函数

        public DeviceTopologyManager(CompositeDeviceManager deviceManager)
        {
            _deviceManager = deviceManager ?? throw new ArgumentNullException(nameof(deviceManager));
            InitializeLayoutAlgorithms();
            InitializeDefaultDiagram();
        }

        #endregion

        #region 公共属性

        /// <summary>
        /// 当前拓扑图ID
        /// </summary>
        public string CurrentDiagramId => _currentDiagramId;

        /// <summary>
        /// 拓扑图数量
        /// </summary>
        public int DiagramCount => _diagrams.Count;

        /// <summary>
        /// 可用的布局算法
        /// </summary>
        public IReadOnlyDictionary<string, ILayoutAlgorithm> LayoutAlgorithms => _layoutAlgorithms;

        #endregion

        #region 初始化方法

        /// <summary>
        /// 初始化布局算法
        /// </summary>
        private void InitializeLayoutAlgorithms()
        {
            var hierarchical = new HierarchicalLayout();
            var forceDirected = new ForceDirectedLayout();

            _layoutAlgorithms[hierarchical.AlgorithmName] = hierarchical;
            _layoutAlgorithms[forceDirected.AlgorithmName] = forceDirected;
        }

        /// <summary>
        /// 初始化默认拓扑图
        /// </summary>
        private void InitializeDefaultDiagram()
        {
            var defaultDiagram = new TopologyDiagram
            {
                DiagramId = "DEFAULT_TOPOLOGY",
                DiagramName = "默认设备拓扑图",
                TopologyType = TopologyType.Hierarchical
            };

            _diagrams[defaultDiagram.DiagramId] = defaultDiagram;
            _currentDiagramId = defaultDiagram.DiagramId;

            // 根据现有设备创建节点
            CreateNodesFromDevices(defaultDiagram);
        }

        /// <summary>
        /// 从设备创建节点
        /// </summary>
        private void CreateNodesFromDevices(TopologyDiagram diagram)
        {
            float x = 100, y = 100;
            int nodeCount = 0;

            foreach (var device in _deviceManager.Devices.Values)
            {
                var node = new TopologyNode
                {
                    NodeId = $"NODE_{device.DeviceId}",
                    NodeName = device.DeviceName,
                    NodeType = GetNodeTypeFromDevice(device),
                    DeviceId = device.DeviceId,
                    Position = new PointF(x + (nodeCount % 5) * 150, y + (nodeCount / 5) * 100),
                    NodeColor = GetColorFromDeviceType(device.DeviceType)
                };

                diagram.Nodes[node.NodeId] = node;
                nodeCount++;
            }
        }

        /// <summary>
        /// 从设备获取节点类型
        /// </summary>
        private NodeType GetNodeTypeFromDevice(ICompositeDevice device)
        {
            return device.DeviceType switch
            {
                CompositeDeviceType.ValveController => NodeType.Actuator,
                CompositeDeviceType.PumpController => NodeType.Actuator,
                CompositeDeviceType.VFDController => NodeType.Controller,
                CompositeDeviceType.TankController => NodeType.Device,
                CompositeDeviceType.HeatExchangerController => NodeType.Device,
                CompositeDeviceType.ReactorController => NodeType.Device,
                _ => NodeType.Device
            };
        }

        /// <summary>
        /// 从设备类型获取颜色
        /// </summary>
        private Color GetColorFromDeviceType(CompositeDeviceType deviceType)
        {
            return deviceType switch
            {
                CompositeDeviceType.ValveController => Color.LightGreen,
                CompositeDeviceType.PumpController => Color.LightBlue,
                CompositeDeviceType.VFDController => Color.Orange,
                CompositeDeviceType.TankController => Color.LightGray,
                CompositeDeviceType.HeatExchangerController => Color.Yellow,
                CompositeDeviceType.ReactorController => Color.Pink,
                _ => Color.LightBlue
            };
        }

        #endregion

        #region 拓扑图管理

        /// <summary>
        /// 创建新的拓扑图
        /// </summary>
        public DeviceOperationResult CreateDiagram(string diagramId, string diagramName, TopologyType topologyType)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(diagramId))
                {
                    return new DeviceOperationResult
                    {
                        Success = false,
                        Message = "拓扑图ID不能为空"
                    };
                }

                if (_diagrams.ContainsKey(diagramId))
                {
                    return new DeviceOperationResult
                    {
                        Success = false,
                        Message = $"拓扑图 '{diagramId}' 已存在"
                    };
                }

                var diagram = new TopologyDiagram
                {
                    DiagramId = diagramId,
                    DiagramName = diagramName,
                    TopologyType = topologyType
                };

                _diagrams[diagramId] = diagram;

                TopologyChanged?.Invoke(this, new TopologyChangedEventArgs
                {
                    DiagramId = diagramId,
                    ChangeType = "DiagramCreated"
                });

                return new DeviceOperationResult
                {
                    Success = true,
                    Message = $"拓扑图 '{diagramId}' 创建成功"
                };
            }
            catch (Exception ex)
            {
                return new DeviceOperationResult
                {
                    Success = false,
                    Message = $"创建拓扑图失败: {ex.Message}",
                    Exception = ex
                };
            }
        }

        /// <summary>
        /// 获取拓扑图
        /// </summary>
        public TopologyDiagram? GetDiagram(string diagramId)
        {
            return _diagrams.TryGetValue(diagramId, out var diagram) ? diagram : null;
        }

        /// <summary>
        /// 获取当前拓扑图
        /// </summary>
        public TopologyDiagram? GetCurrentDiagram()
        {
            return GetDiagram(_currentDiagramId);
        }

        /// <summary>
        /// 设置当前拓扑图
        /// </summary>
        public DeviceOperationResult SetCurrentDiagram(string diagramId)
        {
            if (!_diagrams.ContainsKey(diagramId))
            {
                return new DeviceOperationResult
                {
                    Success = false,
                    Message = $"拓扑图 '{diagramId}' 不存在"
                };
            }

            _currentDiagramId = diagramId;

            return new DeviceOperationResult
            {
                Success = true,
                Message = $"当前拓扑图已切换到 '{diagramId}'"
            };
        }

        /// <summary>
        /// 删除拓扑图
        /// </summary>
        public DeviceOperationResult RemoveDiagram(string diagramId)
        {
            try
            {
                if (!_diagrams.ContainsKey(diagramId))
                {
                    return new DeviceOperationResult
                    {
                        Success = false,
                        Message = $"拓扑图 '{diagramId}' 不存在"
                    };
                }

                if (diagramId == _currentDiagramId)
                {
                    // 如果删除的是当前图，切换到其他图
                    var otherDiagram = _diagrams.Keys.FirstOrDefault(id => id != diagramId);
                    _currentDiagramId = otherDiagram ?? "";
                }

                _diagrams.TryRemove(diagramId, out _);

                TopologyChanged?.Invoke(this, new TopologyChangedEventArgs
                {
                    DiagramId = diagramId,
                    ChangeType = "DiagramRemoved"
                });

                return new DeviceOperationResult
                {
                    Success = true,
                    Message = $"拓扑图 '{diagramId}' 删除成功"
                };
            }
            catch (Exception ex)
            {
                return new DeviceOperationResult
                {
                    Success = false,
                    Message = $"删除拓扑图失败: {ex.Message}",
                    Exception = ex
                };
            }
        }

        #endregion

        #region 节点管理

        /// <summary>
        /// 添加节点
        /// </summary>
        public DeviceOperationResult AddNode(string diagramId, TopologyNode node)
        {
            try
            {
                if (!_diagrams.TryGetValue(diagramId, out var diagram))
                {
                    return new DeviceOperationResult
                    {
                        Success = false,
                        Message = $"拓扑图 '{diagramId}' 不存在"
                    };
                }

                if (string.IsNullOrWhiteSpace(node.NodeId))
                {
                    return new DeviceOperationResult
                    {
                        Success = false,
                        Message = "节点ID不能为空"
                    };
                }

                if (diagram.Nodes.ContainsKey(node.NodeId))
                {
                    return new DeviceOperationResult
                    {
                        Success = false,
                        Message = $"节点 '{node.NodeId}' 已存在"
                    };
                }

                diagram.Nodes[node.NodeId] = node;
                diagram.LastModified = DateTime.Now;

                TopologyChanged?.Invoke(this, new TopologyChangedEventArgs
                {
                    DiagramId = diagramId,
                    ChangeType = "NodeAdded",
                    AffectedNodeId = node.NodeId
                });

                return new DeviceOperationResult
                {
                    Success = true,
                    Message = $"节点 '{node.NodeId}' 添加成功"
                };
            }
            catch (Exception ex)
            {
                return new DeviceOperationResult
                {
                    Success = false,
                    Message = $"添加节点失败: {ex.Message}",
                    Exception = ex
                };
            }
        }

        /// <summary>
        /// 更新节点位置
        /// </summary>
        public DeviceOperationResult UpdateNodePosition(string diagramId, string nodeId, PointF newPosition)
        {
            try
            {
                if (!_diagrams.TryGetValue(diagramId, out var diagram))
                {
                    return new DeviceOperationResult
                    {
                        Success = false,
                        Message = $"拓扑图 '{diagramId}' 不存在"
                    };
                }

                if (!diagram.Nodes.TryGetValue(nodeId, out var node))
                {
                    return new DeviceOperationResult
                    {
                        Success = false,
                        Message = $"节点 '{nodeId}' 不存在"
                    };
                }

                node.Position = newPosition;
                diagram.LastModified = DateTime.Now;

                TopologyChanged?.Invoke(this, new TopologyChangedEventArgs
                {
                    DiagramId = diagramId,
                    ChangeType = "NodeMoved",
                    AffectedNodeId = nodeId
                });

                return new DeviceOperationResult
                {
                    Success = true,
                    Message = $"节点 '{nodeId}' 位置已更新"
                };
            }
            catch (Exception ex)
            {
                return new DeviceOperationResult
                {
                    Success = false,
                    Message = $"更新节点位置失败: {ex.Message}",
                    Exception = ex
                };
            }
        }

        /// <summary>
        /// 删除节点
        /// </summary>
        public DeviceOperationResult RemoveNode(string diagramId, string nodeId)
        {
            try
            {
                if (!_diagrams.TryGetValue(diagramId, out var diagram))
                {
                    return new DeviceOperationResult
                    {
                        Success = false,
                        Message = $"拓扑图 '{diagramId}' 不存在"
                    };
                }

                if (!diagram.Nodes.ContainsKey(nodeId))
                {
                    return new DeviceOperationResult
                    {
                        Success = false,
                        Message = $"节点 '{nodeId}' 不存在"
                    };
                }

                // 删除相关连接
                var relatedConnections = diagram.Connections.Values
                    .Where(c => c.SourceNodeId == nodeId || c.TargetNodeId == nodeId)
                    .Select(c => c.ConnectionId)
                    .ToList();

                foreach (var connectionId in relatedConnections)
                {
                    diagram.Connections.Remove(connectionId);
                }

                diagram.Nodes.Remove(nodeId);
                diagram.LastModified = DateTime.Now;

                TopologyChanged?.Invoke(this, new TopologyChangedEventArgs
                {
                    DiagramId = diagramId,
                    ChangeType = "NodeRemoved",
                    AffectedNodeId = nodeId
                });

                return new DeviceOperationResult
                {
                    Success = true,
                    Message = $"节点 '{nodeId}' 删除成功"
                };
            }
            catch (Exception ex)
            {
                return new DeviceOperationResult
                {
                    Success = false,
                    Message = $"删除节点失败: {ex.Message}",
                    Exception = ex
                };
            }
        }

        #endregion

        #region 连接管理

        /// <summary>
        /// 添加连接
        /// </summary>
        public DeviceOperationResult AddConnection(string diagramId, TopologyConnection connection)
        {
            try
            {
                if (!_diagrams.TryGetValue(diagramId, out var diagram))
                {
                    return new DeviceOperationResult
                    {
                        Success = false,
                        Message = $"拓扑图 '{diagramId}' 不存在"
                    };
                }

                if (string.IsNullOrWhiteSpace(connection.ConnectionId))
                {
                    return new DeviceOperationResult
                    {
                        Success = false,
                        Message = "连接ID不能为空"
                    };
                }

                if (!diagram.Nodes.ContainsKey(connection.SourceNodeId))
                {
                    return new DeviceOperationResult
                    {
                        Success = false,
                        Message = $"源节点 '{connection.SourceNodeId}' 不存在"
                    };
                }

                if (!diagram.Nodes.ContainsKey(connection.TargetNodeId))
                {
                    return new DeviceOperationResult
                    {
                        Success = false,
                        Message = $"目标节点 '{connection.TargetNodeId}' 不存在"
                    };
                }

                diagram.Connections[connection.ConnectionId] = connection;
                diagram.LastModified = DateTime.Now;

                TopologyChanged?.Invoke(this, new TopologyChangedEventArgs
                {
                    DiagramId = diagramId,
                    ChangeType = "ConnectionAdded",
                    AffectedNodeId = connection.ConnectionId
                });

                return new DeviceOperationResult
                {
                    Success = true,
                    Message = $"连接 '{connection.ConnectionId}' 添加成功"
                };
            }
            catch (Exception ex)
            {
                return new DeviceOperationResult
                {
                    Success = false,
                    Message = $"添加连接失败: {ex.Message}",
                    Exception = ex
                };
            }
        }

        /// <summary>
        /// 删除连接
        /// </summary>
        public DeviceOperationResult RemoveConnection(string diagramId, string connectionId)
        {
            try
            {
                if (!_diagrams.TryGetValue(diagramId, out var diagram))
                {
                    return new DeviceOperationResult
                    {
                        Success = false,
                        Message = $"拓扑图 '{diagramId}' 不存在"
                    };
                }

                if (!diagram.Connections.ContainsKey(connectionId))
                {
                    return new DeviceOperationResult
                    {
                        Success = false,
                        Message = $"连接 '{connectionId}' 不存在"
                    };
                }

                diagram.Connections.Remove(connectionId);
                diagram.LastModified = DateTime.Now;

                TopologyChanged?.Invoke(this, new TopologyChangedEventArgs
                {
                    DiagramId = diagramId,
                    ChangeType = "ConnectionRemoved",
                    AffectedNodeId = connectionId
                });

                return new DeviceOperationResult
                {
                    Success = true,
                    Message = $"连接 '{connectionId}' 删除成功"
                };
            }
            catch (Exception ex)
            {
                return new DeviceOperationResult
                {
                    Success = false,
                    Message = $"删除连接失败: {ex.Message}",
                    Exception = ex
                };
            }
        }

        #endregion

        #region 布局算法

        /// <summary>
        /// 应用布局算法
        /// </summary>
        public DeviceOperationResult ApplyLayout(string diagramId, string algorithmName)
        {
            try
            {
                if (!_diagrams.TryGetValue(diagramId, out var diagram))
                {
                    return new DeviceOperationResult
                    {
                        Success = false,
                        Message = $"拓扑图 '{diagramId}' 不存在"
                    };
                }

                if (!_layoutAlgorithms.TryGetValue(algorithmName, out var algorithm))
                {
                    return new DeviceOperationResult
                    {
                        Success = false,
                        Message = $"布局算法 '{algorithmName}' 不存在"
                    };
                }

                algorithm.ApplyLayout(diagram);
                diagram.LastModified = DateTime.Now;

                TopologyChanged?.Invoke(this, new TopologyChangedEventArgs
                {
                    DiagramId = diagramId,
                    ChangeType = "LayoutApplied"
                });

                return new DeviceOperationResult
                {
                    Success = true,
                    Message = $"布局算法 '{algorithmName}' 应用成功"
                };
            }
            catch (Exception ex)
            {
                return new DeviceOperationResult
                {
                    Success = false,
                    Message = $"应用布局算法失败: {ex.Message}",
                    Exception = ex
                };
            }
        }

        /// <summary>
        /// 自动布局当前图
        /// </summary>
        public DeviceOperationResult AutoLayout()
        {
            var diagram = GetCurrentDiagram();
            if (diagram == null)
            {
                return new DeviceOperationResult
                {
                    Success = false,
                    Message = "没有当前拓扑图"
                };
            }

            // 根据拓扑类型选择合适的布局算法
            var algorithmName = diagram.TopologyType switch
            {
                TopologyType.Hierarchical => "分层布局",
                TopologyType.Tree => "分层布局",
                _ => "力导向布局"
            };

            return ApplyLayout(_currentDiagramId, algorithmName);
        }

        #endregion

        #region 查询和分析

        /// <summary>
        /// 查找节点
        /// </summary>
        public TopologyNode? FindNode(string diagramId, string nodeId)
        {
            if (_diagrams.TryGetValue(diagramId, out var diagram))
            {
                return diagram.Nodes.GetValueOrDefault(nodeId);
            }
            return null;
        }

        /// <summary>
        /// 查找设备对应的节点
        /// </summary>
        public List<TopologyNode> FindNodesByDevice(string diagramId, string deviceId)
        {
            if (_diagrams.TryGetValue(diagramId, out var diagram))
            {
                return diagram.Nodes.Values
                    .Where(n => n.DeviceId == deviceId)
                    .ToList();
            }
            return new List<TopologyNode>();
        }

        /// <summary>
        /// 获取节点的邻居
        /// </summary>
        public List<TopologyNode> GetNodeNeighbors(string diagramId, string nodeId)
        {
            if (!_diagrams.TryGetValue(diagramId, out var diagram))
                return new List<TopologyNode>();

            var neighborIds = new HashSet<string>();

            // 查找所有相关连接
            foreach (var connection in diagram.Connections.Values)
            {
                if (connection.SourceNodeId == nodeId)
                    neighborIds.Add(connection.TargetNodeId);
                else if (connection.TargetNodeId == nodeId)
                    neighborIds.Add(connection.SourceNodeId);
            }

            return neighborIds
                .Select(id => diagram.Nodes.GetValueOrDefault(id))
                .Where(node => node != null)
                .Cast<TopologyNode>()
                .ToList();
        }

        /// <summary>
        /// 计算拓扑路径
        /// </summary>
        public List<string> FindPath(string diagramId, string sourceNodeId, string targetNodeId)
        {
            if (!_diagrams.TryGetValue(diagramId, out var diagram))
                return new List<string>();

            // 使用BFS算法查找最短路径
            var queue = new Queue<(string NodeId, List<string> Path)>();
            var visited = new HashSet<string>();

            queue.Enqueue((sourceNodeId, new List<string> { sourceNodeId }));
            visited.Add(sourceNodeId);

            while (queue.Count > 0)
            {
                var (currentNodeId, currentPath) = queue.Dequeue();

                if (currentNodeId == targetNodeId)
                    return currentPath;

                var neighbors = GetNodeNeighbors(diagramId, currentNodeId);
                foreach (var neighbor in neighbors)
                {
                    if (!visited.Contains(neighbor.NodeId))
                    {
                        visited.Add(neighbor.NodeId);
                        var newPath = new List<string>(currentPath) { neighbor.NodeId };
                        queue.Enqueue((neighbor.NodeId, newPath));
                    }
                }
            }

            return new List<string>(); // 没找到路径
        }

        /// <summary>
        /// 分析拓扑结构
        /// </summary>
        public Dictionary<string, object> AnalyzeTopology(string diagramId)
        {
            if (!_diagrams.TryGetValue(diagramId, out var diagram))
                return new Dictionary<string, object>();

            var analysis = new Dictionary<string, object>();

            // 基本统计
            analysis["NodeCount"] = diagram.Nodes.Count;
            analysis["ConnectionCount"] = diagram.Connections.Count;

            // 节点类型统计
            var nodeTypeStats = diagram.Nodes.Values
                .GroupBy(n => n.NodeType)
                .ToDictionary(g => g.Key.ToString(), g => g.Count());
            analysis["NodeTypeStats"] = nodeTypeStats;

            // 连接类型统计
            var connectionTypeStats = diagram.Connections.Values
                .GroupBy(c => c.ConnectionType)
                .ToDictionary(g => g.Key.ToString(), g => g.Count());
            analysis["ConnectionTypeStats"] = connectionTypeStats;

            // 连通性分析
            var components = FindConnectedComponents(diagram);
            analysis["ConnectedComponents"] = components.Count;
            analysis["LargestComponentSize"] = components.Any() ? components.Max(c => c.Count) : 0;

            // 中心性分析
            var centralityScores = CalculateNodeCentrality(diagram);
            analysis["CentralityScores"] = centralityScores;

            return analysis;
        }

        #endregion

        #region 私有分析方法

        /// <summary>
        /// 查找连通分量
        /// </summary>
        private List<List<string>> FindConnectedComponents(TopologyDiagram diagram)
        {
            var visited = new HashSet<string>();
            var components = new List<List<string>>();

            foreach (var nodeId in diagram.Nodes.Keys)
            {
                if (!visited.Contains(nodeId))
                {
                    var component = new List<string>();
                    var stack = new Stack<string>();
                    stack.Push(nodeId);

                    while (stack.Count > 0)
                    {
                        var currentNodeId = stack.Pop();
                        if (!visited.Contains(currentNodeId))
                        {
                            visited.Add(currentNodeId);
                            component.Add(currentNodeId);

                            var neighbors = GetNodeNeighbors(diagram.DiagramId, currentNodeId);
                            foreach (var neighbor in neighbors)
                            {
                                if (!visited.Contains(neighbor.NodeId))
                                {
                                    stack.Push(neighbor.NodeId);
                                }
                            }
                        }
                    }

                    components.Add(component);
                }
            }

            return components;
        }

        /// <summary>
        /// 计算节点中心性
        /// </summary>
        private Dictionary<string, double> CalculateNodeCentrality(TopologyDiagram diagram)
        {
            var centrality = new Dictionary<string, double>();

            foreach (var nodeId in diagram.Nodes.Keys)
            {
                // 计算度中心性（连接数）
                var connectionCount = diagram.Connections.Values
                    .Count(c => c.SourceNodeId == nodeId || c.TargetNodeId == nodeId);

                centrality[nodeId] = connectionCount;
            }

            return centrality;
        }

        #endregion

        #region 导入导出

        /// <summary>
        /// 导出拓扑图
        /// </summary>
        public string ExportDiagram(string diagramId)
        {
            if (_diagrams.TryGetValue(diagramId, out var diagram))
            {
                return JsonConvert.SerializeObject(diagram, Formatting.Indented);
            }
            return "";
        }

        /// <summary>
        /// 导入拓扑图
        /// </summary>
        public DeviceOperationResult ImportDiagram(string diagramJson)
        {
            try
            {
                var diagram = JsonConvert.DeserializeObject<TopologyDiagram>(diagramJson);
                if (diagram == null)
                {
                    return new DeviceOperationResult
                    {
                        Success = false,
                        Message = "拓扑图数据格式无效"
                    };
                }

                _diagrams[diagram.DiagramId] = diagram;

                TopologyChanged?.Invoke(this, new TopologyChangedEventArgs
                {
                    DiagramId = diagram.DiagramId,
                    ChangeType = "DiagramImported"
                });

                return new DeviceOperationResult
                {
                    Success = true,
                    Message = $"拓扑图 '{diagram.DiagramId}' 导入成功"
                };
            }
            catch (Exception ex)
            {
                return new DeviceOperationResult
                {
                    Success = false,
                    Message = $"导入拓扑图失败: {ex.Message}",
                    Exception = ex
                };
            }
        }

        #endregion

        #region 可视化支持

        /// <summary>
        /// 获取节点在指定位置的命中测试
        /// </summary>
        public TopologyNode? HitTestNode(string diagramId, PointF point)
        {
            if (!_diagrams.TryGetValue(diagramId, out var diagram))
                return null;

            foreach (var node in diagram.Nodes.Values.Where(n => n.IsVisible))
            {
                var nodeRect = new RectangleF(
                    node.Position.X - node.Size.Width / 2,
                    node.Position.Y - node.Size.Height / 2,
                    node.Size.Width,
                    node.Size.Height);

                if (nodeRect.Contains(point))
                    return node;
            }

            return null;
        }

        /// <summary>
        /// 选择节点
        /// </summary>
        public void SelectNode(string nodeId)
        {
            NodeSelected?.Invoke(this, nodeId);
        }

        /// <summary>
        /// 获取可视化边界
        /// </summary>
        public RectangleF GetVisualBounds(string diagramId)
        {
            if (!_diagrams.TryGetValue(diagramId, out var diagram))
                return RectangleF.Empty;

            if (!diagram.Nodes.Any())
                return diagram.ViewBounds;

            var minX = diagram.Nodes.Values.Min(n => n.Position.X - n.Size.Width / 2);
            var minY = diagram.Nodes.Values.Min(n => n.Position.Y - n.Size.Height / 2);
            var maxX = diagram.Nodes.Values.Max(n => n.Position.X + n.Size.Width / 2);
            var maxY = diagram.Nodes.Values.Max(n => n.Position.Y + n.Size.Height / 2);

            return new RectangleF(minX - 50, minY - 50, maxX - minX + 100, maxY - minY + 100);
        }

        #endregion
    }
}