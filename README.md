# Collections

[![Test](https://github.com/simulation-tree/collections/actions/workflows/test.yml/badge.svg)](https://github.com/simulation-tree/collections/actions/workflows/test.yml)

Native C# library implementing collections.

### Installation

Install it by cloning it, or referencing through the NuGet package through GitHub's registry ([authentication info](https://docs.github.com/en/packages/working-with-a-github-packages-registry/working-with-the-nuget-registry#authenticating-to-github-packages)).

For installing as a Unity package, use this git url to add it:
```
https://github.com/simulation-tree/collections.git?path=source#unity
```

### Arrays

```cs
using Array<int> array = new(4);
array[0] = 1;
array[1] = 3;
array[2] = 3;
array[3] = 7;

foreach (int item in array)
{
    Console.WriteLine(item);
}
```

### Lists

```cs
using List<int> list = new();
list.Add(1);
list.Add(3);
list.Add(3);
list.Add(7);

foreach (int item in list)
{
    Console.WriteLine(item);
}
```

### Dictionaries

```cs
using Dictionary<byte, int> dictionary = new();
dictionary.Add(1, 1337);
dictionary.Add(2, 8008135);
dictionary.Add(3, 42);

foreach ((byte key, int value) in dictionary)
{
    Console.WriteLine($"{key} = {value}");
}
```

### Stacks

```cs
using Stack<int> stack = new();
stack.Push(1);
stack.Push(3);
stack.Push(3);
stack.Push(7);

while (stack.TryPop(out int item))
{
    Console.WriteLine(item);
}
```

### Queues
    
```cs
using Queue<int> queue = new();
queue.Enqueue(1);
queue.Enqueue(3);
queue.Enqueue(3);
queue.Enqueue(7);

while (queue.TryDequeue(out int item))
{
    Console.WriteLine(item);
}
```

### Hash sets

```cs
using HashSet<int> hashSet = new();
hashSet.TryAdd(1);
hashSet.TryAdd(3);
hashSet.TryAdd(3);
hashSet.TryAdd(7);

foreach (int item in hashSet)
{
    Console.WriteLine(item);
}

if (hashSet.TryGetValue(7, out int actualValue))
{
    Console.WriteLine(actualValue);
}
```
