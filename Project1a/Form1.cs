using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Text.RegularExpressions;

namespace Project1a
{
    struct Node
    {
        public int ID;
        public Point position;
    }

    struct Edge
    {
        public int ID;
        public int node_id_1;
        public int node_id_2;
        public int weight;
    }

    class Graph
    {
        public Dictionary<int, Node> nodes = new Dictionary<int, Node>();
        public Dictionary<int, Edge> edges = new Dictionary<int, Edge>();

        public void Clear()
        {
            nodes.Clear();
            edges.Clear();
        }
    }

    class Forest
    {
        public List<Graph> forest = new List<Graph>();
        public List<Edge> edges;

        public void fill(Graph graph)
        {
            foreach(var node in graph.nodes.Values)
            {
                var local_graph = new Graph();
                local_graph.nodes.Add(node.ID, node);
                forest.Add(local_graph);
            }
            edges = new List<Edge>(graph.edges.Values);
        }

        public Graph generate()
        {
            List<Edge> S = edges.OrderBy(e => e.weight).ToList();
            while (S.Count != 0 && forest.Count > 1)
            {
                Edge smallest = S[0];
                S.RemoveAt(0);
                Graph tree1 = forest.Find(g => g.nodes.ContainsKey(smallest.node_id_1));
                Graph tree2 = forest.Find(g => g.nodes.ContainsKey(smallest.node_id_2));
                if (tree1 != tree2)
                {
                    forest.Remove(tree2);
                    tree2.nodes.ToList().ForEach(pair => tree1.nodes.Add(pair.Key, pair.Value));
                    tree2.edges.ToList().ForEach(pair => tree1.edges.Add(pair.Key, pair.Value));
                    tree1.edges.Add(smallest.ID, smallest);
                }
            }
            if (forest.Count > 1)
            {
                Console.Out.WriteLine("Error..");
            }
            return forest[0];
        }
    }

   

    public partial class Form1 : Form
    {
        Graph graph = new Graph();
        Graph spanning_tree;
        public Form1()
        {
            InitializeComponent();
        }

        bool ReadGraph(string content)
        {
            graph.Clear();
            var lines = content.Split('\n');

            var number_nodes_regex = new Regex(@"WEZLY = (\d+)", RegexOptions.IgnoreCase);
            var number_edge_regex = new Regex(@"LACZA = (\d+)", RegexOptions.IgnoreCase);
            var node_regex = new Regex(@"(\d+) (\d+) (\d+)", RegexOptions.IgnoreCase);
            var edge_regex = new Regex(@"(\d+) (\d+) (\d+) (\d+)", RegexOptions.IgnoreCase);

            int number_nodes = -1;
            int number_edges = -1;
            bool read_nodes = true;

            foreach (string line in lines)
            {
                if (line.StartsWith("#")) continue;
                if (line.Trim().Length == 0) continue;
                if (read_nodes)
                {
                    if(number_nodes < 0)
                    {
                        var match = number_nodes_regex.Match(line);
                        if (match.Success)
                        {
                            number_nodes = int.Parse(match.Groups[1].Value);
                            continue;
                        }
                        else
                            return false;
                    }

                    {
                        var match = number_edge_regex.Match(line);
                        if (match.Success)
                        {
                            number_edges = int.Parse(match.Groups[1].Value);
                            read_nodes = false;
                            continue;
                        }
                    }

                    {
                        var match = node_regex.Match(line);
                        if (match.Success)
                        {
                            Node node = new Node();
                            node.ID = int.Parse(match.Groups[1].Value);
                            node.position.X = int.Parse(match.Groups[2].Value);
                            node.position.Y = int.Parse(match.Groups[3].Value);
                            graph.nodes.Add(node.ID, node);
                        }
                        else
                            return false;
                    }
                }
                else
                {
                    var match = edge_regex.Match(line);
                    if (match.Success)
                    {
                        Edge edge = new Edge();
                        edge.ID = int.Parse(match.Groups[1].Value);
                        edge.node_id_1 = int.Parse(match.Groups[2].Value);
                        edge.node_id_2 = int.Parse(match.Groups[3].Value);
                        edge.weight = int.Parse(match.Groups[4].Value);
                        graph.edges.Add(edge.ID, edge);
                    }
                    else
                        return false;
                }
            }

            if(number_edges != graph.edges.Count)
            {
                return false;
            }

            if (number_nodes != graph.nodes.Count)
            {
                return false;
            }

            return true;
        }

        Point ScalePoint(Point p)
        {
            int location_scale = 5;
            return new Point(p.X * location_scale, p.Y * location_scale);
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {
            var regular_node_pen = new Pen(Brushes.Blue);
            Size node_size = new Size(20, 20);
            Size half_size = new Size(10,10);

            e.Graphics.Clear(Color.White);
            foreach (var node in graph.nodes.Values)
            {
                e.Graphics.DrawRectangle(regular_node_pen, new Rectangle(ScalePoint(node.position), node_size));
            }

            var regular_edge_pen = new Pen(Brushes.Red);
            var spanning_edge_pen = new Pen(Brushes.Green);

            foreach (var edge in graph.edges.Values)
            {
                Point pos1 = ScalePoint(graph.nodes[edge.node_id_1].position) + half_size;
                Point pos2 = ScalePoint(graph.nodes[edge.node_id_2].position) + half_size;

                bool from_spanning_tree = spanning_tree.edges.ContainsKey(edge.ID);
                e.Graphics.DrawLine(from_spanning_tree ? spanning_edge_pen : regular_edge_pen, pos1, pos2);

                e.Graphics.DrawString(edge.weight.ToString(), DefaultFont, Brushes.Black
                    , new PointF((pos1.X + pos2.X) / 2.0f, (pos1.Y + pos2.Y) / 2.0f));
            }
        }

        private void wczytajToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog1.ShowDialog();
        }

        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
            using (StreamReader sr = new StreamReader(openFileDialog1.FileName))
            {
                String content = sr.ReadToEnd();
                Console.WriteLine(content);
                if(!ReadGraph(content))
                {
                    Console.WriteLine("Cannot Read File: " + openFileDialog1.FileName);
                    return;
                }
            }

            {
                Forest kruskal = new Forest();
                kruskal.fill(graph);
                spanning_tree = kruskal.generate();
            }

            panel1.Invalidate();
        }
    }
}
