using System.Collections.Generic;

public class UnionFind<T>
{
    Dictionary<T, T> parent =  new Dictionary<T, T>();
    Dictionary<T, int> size = new Dictionary<T, int>();  // Size of trees
    public T Find(T a)
    {
        if (!parent.ContainsKey(a)) {
            parent[a] = a;
            size[a] = 1;
        }
        if (EqualityComparer<T>.Default.Equals(a, parent[a])) return a;
        size.Remove(a);
        T result = Find(parent[a]);
        parent[a] = result;  // Compression
        return result;
    }

    public void Union(T a, T b)
    {
        T aParent = Find(a);
        T bParent = Find(b);
        if (EqualityComparer<T>.Default.Equals(aParent, bParent)) return;
        if (size[aParent] > size[bParent]) {
            parent[bParent] = aParent;  // Move b tree under a tree
            size[aParent] += size[bParent];
            size.Remove(bParent);  // No longer a root
        } else {
            parent[aParent] = bParent;
            size[bParent] += size[aParent];
            size.Remove(aParent);
        }
    }
}
