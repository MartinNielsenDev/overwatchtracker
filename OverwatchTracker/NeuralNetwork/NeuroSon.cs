using System;
using System.Collections.Generic;
using System.IO;
using NeuralNetwork.Adaline;
using NeuralNetwork.Patterns;

namespace NeuralNetwork.Son
{
    /// <summary>
    ///     Implements a node in <see cref="xpidea.neuro.net.son.SelfOrganizingNetwork" />
    /// </summary>
    public class SelfOrganizingNode : NeuroNode
    {
        /// <summary>
        ///     Stores  node learning rate.
        /// </summary>
        public double LearningRate;

        /// <summary>
        ///     Constructs the node and defines the <see cref="xpidea.neuro.net.son.SelfOrganizingNode.LearningRate" />
        /// </summary>
        /// <param name="learningRate">The learning rate of the node.</param>
        public SelfOrganizingNode(double learningRate)
        {
            LearningRate = learningRate;
        }

        /// <summary>
        ///     Overridden.Runs the node.
        /// </summary>
        public override void Run()
        {
            double total = 0;
            foreach (var link in InLinks)
            {
                total += Math.Pow(link.InNode.Value - link.Weight, 2);
            }
            Value = Math.Sqrt(total);
        }

        /// <summary>
        ///     Overridden.Teaches the node.
        /// </summary>
        public override void Learn()
        {
            foreach (var link in InLinks)
            {
                var delta = LearningRate*(link.InNode.Value - link.Weight);
                link.UpdateWeight(delta);
            }
        }
    }

    /// <summary>
    ///     Represents a link in the <see cref="xpidea.neuro.net.son.SelfOrganizingNetwork" /> network.
    /// </summary>
    public class SelfOrganizingLink : AdalineLink
    {
    }

    /// <summary>
    ///     Implements the Self Organizing Network (SON).
    /// </summary>
    /// <remarks>
    ///     <img src="SON.jpg"></img>
    ///     The basic Self-Organizing Network  can be visualized as a sheet-like neural-network array , the cells (or nodes) of
    ///     which become specifically tuned to various input signal patterns or classes of patterns in an orderly fashion. The
    ///     learning process is competitive and unsupervised, meaning that no teacher is needed to define the correct output
    ///     (or actually the cell into which the input is mapped) for an input. In the basic version, only one map node
    ///     (winner) at a time is activated corresponding to each input. The locations of the responses in the array tend to
    ///     become ordered in the learning process as if some meaningful nonlinear coordinate system for the different input
    ///     features were being created over the network (Kohonen, 1995c).The SOM was developed by Prof. Teuvo Kohonen in the
    ///     early 1980s. The first application area of the SOM was speech recognition, or perhaps more accurately,
    ///     speech-to-text transformation.   (Timo Honkela)
    /// </remarks>
    public class SelfOrganizingNetwork : AdalineNetwork
    {
        /// <summary>
        ///     Number of colums in output layer.
        /// </summary>
        protected int columsCount;

        /// <summary>
        ///     Current iteration.
        /// </summary>
        protected long currentIteration;

        /// <summary>
        ///     Current neighborhood size.
        /// </summary>
        protected int currentNeighborhoodSize;

        /// <summary>
        ///     Final learning rate.
        /// </summary>
        protected double finalLearningRate;

        /// <summary>
        ///     Initial learning rate.
        /// </summary>
        protected double initialLearningRate;

        /// <summary>
        ///     Initial neighborhood size.
        /// </summary>
        protected int initialNeighborhoodSize;

        /// <summary>
        ///     Represents the Kohonen layer as two-dimetional array of <see cref="xpidea.neuro.net.NeuroNode" />.
        /// </summary>
        protected NeuroNode[,] kohonenLayer;

        /// <summary>
        ///     Neighborhood reduce interval.
        /// </summary>
        protected int neighborhoodReduceInterval;

        /// <summary>
        ///     Number of rows in output layer.
        /// </summary>
        protected int rowsCount;

        /// <summary>
        ///     Number of training iterations.
        /// </summary>
        protected long trainingIterations;

        /// <summary>
        ///     Winning column in output layer.
        /// </summary>
        protected int winnigCol;

        /// <summary>
        ///     Winning row in output layer.
        /// </summary>
        protected int winnigRow;

        /// <summary>
        ///     Constructs the network.
        /// </summary>
        /// <param name="aInputNodesCount">Number of input nodes.</param>
        /// <param name="aRowCount">Number of rows in output layer.</param>
        /// <param name="aColCount">Number of colums in output layer.</param>
        /// <param name="aInitialLearningRate">Starting learning rate.</param>
        /// <param name="aFinalLearningRate">Ending learning rate.</param>
        /// <param name="aInitialNeighborhoodSize">Initial neighborhood size.</param>
        /// <param name="aNeighborhoodReduceInterval">Number of training iterations after neighborhood size will be reduced.</param>
        /// <param name="aTrainingIterationsCount">Number of training iterations.</param>
        public SelfOrganizingNetwork(int aInputNodesCount, int aRowCount, int aColCount,
            double aInitialLearningRate, double aFinalLearningRate,
            int aInitialNeighborhoodSize, int aNeighborhoodReduceInterval,
            long aTrainingIterationsCount)
        {
            nodesCount = 0;
            linksCount = 0;
            initialLearningRate = aInitialLearningRate;
            finalLearningRate = aFinalLearningRate;
            learningRate = aInitialLearningRate;
            initialNeighborhoodSize = aInitialNeighborhoodSize;
            neighborhoodReduceInterval = aNeighborhoodReduceInterval;
            trainingIterations = aTrainingIterationsCount;
            currentIteration = 0;
            nodesCount = aInputNodesCount;
            currentNeighborhoodSize = initialNeighborhoodSize;
            rowsCount = aRowCount;
            columsCount = aColCount;
            CreateNetwork();
        }
        public SelfOrganizingNetwork()
        {
            nodesCount = 0;
            linksCount = 0;
        }
        public SelfOrganizingNetwork(double[] neuralNetworkData) : base(neuralNetworkData)
        {
        }
        public int KohonenRowsCount
        {
            get { return rowsCount; }
        }

        /// <summary>
        ///     Number of colums in Kohonen layer.
        /// </summary>
        public int KohonenColumsCount
        {
            get { return columsCount; }
        }

        /// <summary>
        ///     Current neighborhood size.
        /// </summary>
        public int CurrentNeighborhoodSize
        {
            get { return currentNeighborhoodSize; }
        }

        /// <summary>
        ///     Array of nodes representing Kohonen layer.
        /// </summary>
        public NeuroNode[,] KohonenNode
        {
            get { return kohonenLayer; }
        }

        /// <summary>
        ///     Winning row index.
        /// </summary>
        public int WinnigRow
        {
            get { return winnigRow; }
        }

        /// <summary>
        ///     Winning column index.
        /// </summary>
        public int WinnigCol
        {
            get { return winnigCol; }
        }

        /// <summary>
        ///     Overridden.Returns <see cref="xpidea.neuro.net.NeuralNetworkType.nntSON" /> for SON network.
        /// </summary>
        /// <returns>Network type.</returns>
        protected override NeuralNetworkType GetNetworkType()
        {
            return NeuralNetworkType.nntSON;
        }

        /// <summary>
        ///     Overridden.Constructs network topology.
        /// </summary>
        protected override void CreateNetwork()
        {
            nodes = new NeuroNode[NodesCount];
            linksCount = NodesCount*rowsCount*columsCount;
            kohonenLayer = new NeuroNode[rowsCount, columsCount];
            links = new NeuroLink[LinksCount];
            for (var i = 0; i < NodesCount; i++)
                nodes[i] = new InputNode();
            var curr = 0;
            for (var row = 0; row < rowsCount; row++)
                for (var col = 0; col < columsCount; col++)
                {
                    kohonenLayer[row, col] = new SelfOrganizingNode(learningRate);
                    for (var i = 0; i < NodesCount; i++)
                    {
                        links[curr] = new SelfOrganizingLink();
                        nodes[i].LinkTo(kohonenLayer[row, col], links[curr]);
                        curr++;
                    }
                }
        }

        /// <summary>
        ///     Overridden.Returns number of nodes in input layer.
        /// </summary>
        /// <returns>Nodes count.</returns>
        protected override int GetInputNodesCount()
        {
            return NodesCount;
        }

        /// <summary>
        ///     Overridden.Retrieves the input node by its index.
        /// </summary>
        /// <param name="index">Input node index.</param>
        /// <returns>Input node.</returns>
        protected override NeuroNode GetInputNode(int index)
        {
            if ((index >= InputNodesCount) || (index < 0))
                throw new ENeuroException("InputNode index out of bounds.");
            return nodes[index];
        }

        /// <summary>
        ///     Overridden.Returns an output node by its index.
        /// </summary>
        /// <param name="index">Output node index.</param>
        /// <returns>An output node.</returns>
        protected override NeuroNode GetOutputNode(int index)
        {
            return null;
        }

        /// <summary>
        ///     Overridden.Number of nodes in output layer. Always return 0 since there are no nodes as its have an Kohonen layer.
        /// </summary>
        /// <returns></returns>
        protected override int GetOutPutNodesCount()
        {
            return 0;
        }

        /// <summary>
        ///     Overridden.Epoch - number of patterns that was exposed to a network during one training cycle.
        /// </summary>
        /// <param name="epoch"></param>
        public override void Epoch(int epoch)
        {
            currentIteration++;
            learningRate = initialLearningRate -
                           ((currentIteration/(double) trainingIterations)*(initialLearningRate - finalLearningRate));
            if (((((currentIteration + 1)%neighborhoodReduceInterval) == 0) && (currentNeighborhoodSize > 0)))
                currentNeighborhoodSize--;
        }

        /// <summary>
        ///     Overridden.Always returns 0. There is no output node.
        /// </summary>
        /// <returns></returns>
        protected override double GetNodeError()
        {
            return 0;
        }
        protected override void SetNodeError(double value)
        {
            //Cannot set the errors. Nothing is here....
        }
        public override void Load(double[] loadData)
        {
            initialLearningRate = ExtractDataFromArray(loadData);
            finalLearningRate = ExtractDataFromArray(loadData);
            initialNeighborhoodSize = Convert.ToInt32(ExtractDataFromArray(loadData));
            neighborhoodReduceInterval = Convert.ToInt32(ExtractDataFromArray(loadData));
            trainingIterations = Convert.ToInt64(ExtractDataFromArray(loadData));
            rowsCount = Convert.ToInt32(ExtractDataFromArray(loadData));
            columsCount = Convert.ToInt32(ExtractDataFromArray(loadData));
            base.Load(loadData);

            for (var r = 0; r < rowsCount; r++)
            {
                for (var c = 0; c < columsCount; c++)
                {
                    kohonenLayer[r, c].Load(loadData);
                }
            }
        }
        public override void Save(BinaryWriter binaryWriter, List<double> saveData)
        {
            binaryWriter.Write(initialLearningRate);
            binaryWriter.Write(finalLearningRate);
            binaryWriter.Write(initialNeighborhoodSize);
            binaryWriter.Write(neighborhoodReduceInterval);
            binaryWriter.Write(trainingIterations);
            binaryWriter.Write(rowsCount);
            binaryWriter.Write(columsCount);

            saveData.Add(initialLearningRate);
            saveData.Add(finalLearningRate);
            saveData.Add(initialNeighborhoodSize);
            saveData.Add(neighborhoodReduceInterval);
            saveData.Add(trainingIterations);
            saveData.Add(rowsCount);
            saveData.Add(columsCount);

            base.Save(binaryWriter, saveData);
            for (var r = 0; r < rowsCount; r++)
            {
                for (var c = 0; c < columsCount; c++)
                {
                    kohonenLayer[r, c].Save(binaryWriter, saveData);
                }
            }
        }
        public override void Run()
        {
            var minValue = double.PositiveInfinity;
            LoadInputs();
            for (var row = 0; row < rowsCount; row++)
                for (var col = 0; col < columsCount; col++)
                {
                    kohonenLayer[row, col].Run();
                    var nodeValue = kohonenLayer[row, col].Value;
                    if (nodeValue < minValue)
                    {
                        minValue = nodeValue;
                        winnigRow = row;
                        winnigCol = col;
                    }
                }
        }

        /// <summary>
        ///     Overridden.Teaches the network.
        /// </summary>
        public override void Learn()
        {
            var startRow = winnigRow - currentNeighborhoodSize;
            var endRow = winnigRow + currentNeighborhoodSize;
            var startCol = winnigCol - currentNeighborhoodSize;
            var endCol = winnigCol + currentNeighborhoodSize;
            if (startRow < 0) startRow = 0;
            if (startCol < 0) startCol = 0;
            if (endRow >= rowsCount) endRow = rowsCount - 1;
            if (endCol >= columsCount) endCol = columsCount - 1;
            for (var row = startRow; row <= endRow; row++)
                for (var col = startCol; col <= endCol; col++)
                {
                    var node = (SelfOrganizingNode) kohonenLayer[row, col];
                    node.LearningRate = learningRate;
                    node.Learn();
                }
        }

        /// <summary>
        ///     Overridden.Trains the network.
        /// </summary>
        /// <param name="patterns"></param>
        public override void Train(PatternsCollection patterns)
        {
            if (patterns != null)
                for (var i = 0; i < trainingIterations; i++)
                {
                    for (var j = 0; j < patterns.Count; j++)
                    {
                        SetValuesFromPattern(patterns[j]);
                        Run();
                        Learn();
                    }
                    Epoch(0);
                }
        }
    }
}