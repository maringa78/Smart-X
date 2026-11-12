namespace SmartX.API.Services
{
    /// <summary>
    /// A self-balancing binary search tree (AVL tree).
    /// After every insert the tree checks the height difference between each node's two
    /// sides and rotates when it is more than 1, so the tree never leans to one side.
    /// That keeps its height at about log2(n), which means inserting a reading and
    /// finding a time range both stay fast even with hundreds of thousands of readings.
    /// </summary>
    public class AvlTree<TKey, TValue> where TKey : IComparable<TKey>
    {
        private class Node(TKey key, TValue value)
        {
            public TKey Key = key;
            public TValue Value = value;
            public int Height = 1;
            public Node? Left;
            public Node? Right;
        }

        private Node? _root;

        public int Count { get; private set; }

        /// <summary>Number of levels in the tree (about log2 of Count when balanced).</summary>
        public int Height => HeightOf(_root);

        // ---------- insert ----------

        /// <summary>Adds an item. Equal keys are allowed and are placed to the right.</summary>
        public void Insert(TKey key, TValue value)
        {
            _root = Insert(_root, key, value);
            Count++;
        }

        private static Node Insert(Node? node, TKey key, TValue value)
        {
            if (node is null) return new Node(key, value);

            if (key.CompareTo(node.Key) < 0)
                node.Left = Insert(node.Left, key, value);
            else
                node.Right = Insert(node.Right, key, value);

            return Rebalance(node);
        }

        // ---------- range query ----------

        /// <summary>
        /// Returns every value whose key lies between from and to (inclusive), in key order.
        /// Whole branches that cannot contain a match are skipped, so the cost is about
        /// O(log n + k) for k results, instead of scanning all n items.
        /// nodesVisited reports how many nodes were actually looked at.
        /// </summary>
        public List<TValue> RangeQuery(TKey from, TKey to, out int nodesVisited)
        {
            var results = new List<TValue>();
            var visited = 0;
            Collect(_root, from, to, results, ref visited);
            nodesVisited = visited;
            return results;
        }

        private static void Collect(Node? node, TKey from, TKey to, List<TValue> results, ref int visited)
        {
            if (node is null) return;
            visited++;

            var afterFrom = from.CompareTo(node.Key) <= 0;   // node.Key >= from
            var beforeTo = to.CompareTo(node.Key) >= 0;      // node.Key <= to

            // Smaller keys sit on the left, so skip that side if this key is already below the range.
            if (afterFrom) Collect(node.Left, from, to, results, ref visited);

            if (afterFrom && beforeTo) results.Add(node.Value);

            // Larger keys sit on the right, so skip that side if this key is already above the range.
            if (beforeTo) Collect(node.Right, from, to, results, ref visited);
        }

        // ---------- balancing ----------

        private static int HeightOf(Node? node) => node?.Height ?? 0;

        private static void UpdateHeight(Node node) =>
            node.Height = 1 + Math.Max(HeightOf(node.Left), HeightOf(node.Right));

        // Positive: left side is taller. Negative: right side is taller.
        private static int BalanceOf(Node node) => HeightOf(node.Left) - HeightOf(node.Right);

        private static Node RotateRight(Node y)
        {
            var x = y.Left!;
            y.Left = x.Right;
            x.Right = y;
            UpdateHeight(y);
            UpdateHeight(x);
            return x;
        }

        private static Node RotateLeft(Node x)
        {
            var y = x.Right!;
            x.Right = y.Left;
            y.Left = x;
            UpdateHeight(x);
            UpdateHeight(y);
            return y;
        }

        private static Node Rebalance(Node node)
        {
            UpdateHeight(node);
            var balance = BalanceOf(node);

            if (balance > 1)   // left side too tall
            {
                if (BalanceOf(node.Left!) < 0)          // left-right case
                    node.Left = RotateLeft(node.Left!);
                return RotateRight(node);               // left-left case
            }

            if (balance < -1)  // right side too tall
            {
                if (BalanceOf(node.Right!) > 0)         // right-left case
                    node.Right = RotateRight(node.Right!);
                return RotateLeft(node);                // right-right case
            }

            return node;
        }
    }
}