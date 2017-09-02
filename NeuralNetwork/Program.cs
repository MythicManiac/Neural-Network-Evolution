using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeuralNetwork
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var history = new List<double[]>();

            var population = Population.CreateInitialPopulation(32);
            for (int i = 0; i < 1000 && population.GetHighestFitness() < -0.1; i++)
            {
                history.Add(new double[] { population.GetHighestFitness(), population.GetAverageFitness(), population.GetLowestFitness() });
                population = Population.CreateSelectionPopulation(population);
                Console.WriteLine("Generation: {0}", i);
            }
            history.Add(new double[] { population.GetHighestFitness(), population.GetAverageFitness(), population.GetLowestFitness() });
            OutputData(history);
            OutputGenome(population);
        }

        static void OutputGenome(Population pop)
        {
            var stream = new FileStream("genome.dat", FileMode.Create);
            pop.GetHighestFitness();
            var champion = pop.Units[0];
            stream.Write(pop.Units[0].Genes, 0, pop.Units[0].Genes.Length);
            stream.Flush();
            stream.Close();
        }

        static void OutputData(List<double[]> data)
        {
            var stream = new FileStream("graph.dat", FileMode.Create);
            var writer = new StreamWriter(stream);

            var template = @"
var data = {{
    labels: {0},
    datasets: [
        {{
            label: ""Population highest"",
            fillColor: ""rgba(52, 152, 219, 0.2)"",
            strokeColor: ""rgba(52, 152, 219, 1)"",
            pointColor: ""rgba(52, 152, 219, 1)"",
            pointStrokeColor: ""#fff"",
            pointHighlightColor: ""#fff"",
            pointHighlightFill: ""rgba(52, 152, 219, 1)"",
            data: {1},
        }},
        {{
            label: ""Population average"",
            fillColor: ""rgba(155, 89, 182, 0.2)"",
            strokeColor: ""rgba(155, 89, 182, 1)"",
            pointColor: ""rgba(155, 89, 182, 1)"",
            pointStrokeColor: ""#fff"",
            pointHighlightColor: ""#fff"",
            pointHighlightFill: ""rgba(155, 89, 182, 1)"",
            data: {2},
        }},
        {{
            label: ""Population lowest"",
            fillColor: ""rgba(46, 204, 113, 0.2)"",
            strokeColor: ""rgba(46, 204, 113, 1)"",
            pointColor: ""rgba(46, 204, 113, 1)"",
            pointStrokeColor: ""#fff"",
            pointHighlightColor: ""#fff"",
            pointHighlightFill: ""rgba(46, 204, 113, 1)"",
            data: {3},
        }}
    ]
}};
";

            var labels = "[";
            for (int i = 0; i < data.Count; i++)
            {
                labels += string.Format("{0}, ", i);
            }
            labels = labels.Substring(0, labels.Length - 2);
            labels += "]";

            var datastrings = new string[] { "", "", "" };
            for (int i = 0; i < 3; i++)
            {
                datastrings[i] = "[";
                for (int j = 0; j < data.Count; j++)
                {
                    datastrings[i] += string.Format("{0}, ", data[j][i]);
                }
                datastrings[i] = datastrings[i].Substring(0, datastrings[i].Length - 2);
                datastrings[i] += "]";
            }

            writer.Write(string.Format(template, labels, datastrings[0], datastrings[1], datastrings[2]));
            writer.Flush();
            writer.Close();
        }
    }

    public class Population
    {
        public int Size { get; protected set; }
        public Unit[] Units { get; protected set; }
        public int Generation { get; protected set; }

        private bool _sorted;

        private Population(int size, int generation)
        {
            Size = size;
            Units = new Unit[size];
            Generation = generation;
            _sorted = false;
        }

        public void SortByFitness()
        {
            if (_sorted) return;

            for (int i = 0; i < Units.Length; i++)
            {
                if (i == 0) continue;

                var j = i;
                while (Units[j].GetFitness() > Units[j - 1].GetFitness())
                {
                    var temp = Units[j - 1];
                    Units[j - 1] = Units[j];
                    Units[j] = temp;

                    j--;
                    if (j == 0) break;
                }
            }

            _sorted = true;
        }

        public double GetHighestFitness()
        {
            if (!_sorted) SortByFitness();
            return Units[0].GetFitness();
        }

        public double GetAverageFitness()
        {
            double total = 0;
            for (int i = 0; i < Units.Length; i++)
            {
                total += Units[i].GetFitness();
            }
            return total / Units.Length;
        }

        public double GetLowestFitness()
        {
            if (!_sorted) SortByFitness();
            return Units[Units.Length - 1].GetFitness();
        }

        public static Population CreateSelectionPopulation(Population old)
        {
            old.SortByFitness();
            var count = (int)Math.Floor((double)old.Units.Length / 10);
            var candidates = new Unit[count];
            Array.Copy(old.Units, candidates, count);

            var population = new Population(old.Size, old.Generation + 1);
            var random = new Random();
            population.Units[0] = candidates[0];
            for (int i = 1; i < old.Size; i++)
            {
                population.Units[i] = Unit.CreateCrossoverUnit(candidates[random.Next(candidates.Length)], candidates[random.Next(candidates.Length)]);
            }

            return population;
        }

        public static Population CreateInitialPopulation(int size)
        {
            var population = new Population(size, 0);
            for (int i = 0; i < population.Units.Length; i++)
            {
                population.Units[i] = Network.CreateRandomUnit();
            }
            return population;
        }
    }

    public class Unit
    {
        public static Random Rng = new Random();
        public static double MutationRate = 40;
        public static double MutationChance = 0.30;

        public byte[] Genes { get; protected set; }

        protected Unit(byte[] genes)
        {
            Genes = genes;
        }

        public virtual double GetFitness() { return 0.0; }

        public static Unit CreateCrossoverUnit(Unit u1, Unit u2)
        {
            var result = new byte[u1.Genes.Length];
            var cutoff = Rng.Next(1, u1.Genes.Length);
            for (int i = 0; i < u1.Genes.Length; i++)
            {
                if (i < cutoff) result[i] = u1.Genes[i];
                else result[i] = u2.Genes[i];

                if (Rng.NextDouble() >= 1.0 - MutationChance) result[i] += (byte)(Rng.NextDouble() * 255 / MutationRate);
                else if (Rng.NextDouble() <= MutationChance) result[i] -= (byte)(Rng.NextDouble() * 255 / MutationRate);
            }
            return new Network(result);
        }

        public static Unit CreateRandomUnit(int geneCount)
        {
            var genes = new byte[geneCount];
            Rng.NextBytes(genes);
            return new Unit(genes);
        }
    }

    public class Network : Unit
    {
        public Node[] inputLayer;
        public Node[] hiddenLayer;
        public Node[] outputLayer;
        public List<Connection> connections;

        private int _genePointer;

        public static Unit CreateRandomUnit()
        {
            // (inputCount * hiddenCount) + hiddenCount + (hiddenCount * outputcount) + outputCount
            // 43
            var genes = new byte[43];
            Rng.NextBytes(genes);
            return new Network(genes);
        }

        public double GetNextGene()
        {
            var gene = Genes[_genePointer++];
            return (double)gene / 255 * 10;
        }

        public Network(byte[] genes) : base(genes)
        {
            _genePointer = 0;
            inputLayer = new Node[4];
            hiddenLayer = new Node[5];
            outputLayer = new Node[3];
            connections = new List<Connection>();

            for(int i = 0; i < inputLayer.Length; i++)
            {
                inputLayer[i] = new InputNode(this);
            }

            for(int i = 0; i < hiddenLayer.Length; i++)
            {
                hiddenLayer[i] = new Node(this);
            }

            for(int i = 0; i < outputLayer.Length; i++)
            {
                outputLayer[i] = new Node(this);
            }

            ConnectLayers();
        }

        public override double GetFitness()
        {
            return -FindError();
        }

        private double FindError()
        {
            var outputMap = new double[][] {
                new double[] { 1.0, 0.0, 0.0 },
                new double[] { 0.0, 1.0, 0.0 },
                new double[] { 0.0, 0.0, 1.0 }
            };

            var totalError = 0.0d;
            var region = 0;
            for (int i = region; i < region + 80; i++)
            {
                var input = new double[] {
                    Dataset.Data[i][0],
                    Dataset.Data[i][1],
                    Dataset.Data[i][2],
                    Dataset.Data[i][3],
                };
                var output = outputMap[(int)Dataset.Data[i][4] - 1];

                InputData(input);
                ActivateNetwork();
                totalError += ErrorOutput(output);
            }

            return totalError / 30;
        }

        private double ErrorOutput(double[] output)
        {
            var error = 0.0d;
            for(int i = 0; i < outputLayer.Length; i++)
            {
                error += Math.Abs(output[i] - outputLayer[i].Value) / 1.0d;
            }
            return error / output.Length;
        }

        private void ConnectLayers()
        {
            for(int i = 0; i < inputLayer.Length; i++)
            {
                for (int j = 0; j < hiddenLayer.Length; j++)
                {
                    inputLayer[i].ConnectTo(hiddenLayer[j], GetNextGene());
                }
            }

            for(int i = 0; i < hiddenLayer.Length; i++)
            {
                for(int j = 0; j < outputLayer.Length; j++)
                {
                    hiddenLayer[i].ConnectTo(outputLayer[j], GetNextGene());
                }
            }
        }

        public void InputData(double[] input)
        {
            if (input.Length != inputLayer.Length)
                throw new Exception("Input length must match the input layer's node count");

            for (int i = 0; i < inputLayer.Length; i++)
            {
                ((InputNode)inputLayer[i]).SetValue(input[i]);
            }
        }

        private void ActivateNetwork()
        {
            for(int i = 0; i < hiddenLayer.Length; i++)
            {
                hiddenLayer[i].Activate();
            }
            for(int i = 0; i < outputLayer.Length; i++)
            {
                outputLayer[i].Activate();
            }
        }
    }

    public class Node
    {
        public Network Host { get; protected set; }
        public List<Connection> InboundConnections { get; set; }
        public List<Connection> OutboundConnections { get; set; }
        public InputNode Bias { get; set; }
        public double Value { get; protected set; }


        public Node(Network host, bool bias = true)
        {
            Host = host;
            InboundConnections = new List<Connection>();
            OutboundConnections = new List<Connection>();
            if(bias) CreateBias();
        }

        private void CreateBias()
        {
            Bias = new InputNode(Host);
            Bias.SetValue(1.0);
            Bias.ConnectTo(this, Host.GetNextGene());
        }

        public void ConnectTo(Node target, double weight)
        {
            var connection = new Connection(this, target, weight);
            OutboundConnections.Add(connection);
            target.InboundConnections.Add(connection);
            Host.connections.Add(connection);
        }

        private double GetInput()
        {
            var value = 0.0d;
            for(int i = 0; i < InboundConnections.Count; i++)
            {
                value += InboundConnections[i].Source.Value * InboundConnections[i].Weight;
            }
            return value;
        }

        public void Activate()
        {
            var value = GetInput();
            Value = Activation(value);
        }

        private double Activation(double x)
        {
            if (x < -45.0) return 0.0;
            if (x > 45.0) return 1.0;
            return 1.0 / (1.0 + Math.Exp(-x));
        }
    }

    public class InputNode : Node
    {
        public InputNode(Network host) : base(host, false) { }

        public void SetValue(double value)
        {
            Value = value;
        }
    }

    public class Connection
    {
        public Node Source { get; set; }
        public Node Target { get; set; }
        public double Weight { get; set; }
        
        public Connection(Node source, Node target, double weight)
        {
            Source = source;
            Target = target;
            Weight = weight;
        }
    }
}
