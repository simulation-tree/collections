# Collections

[![Test](https://github.com/simulation-tree/collections/actions/workflows/test.yml/badge.svg)](https://github.com/simulation-tree/collections/actions/workflows/test.yml)

Native C# library implementing collections.

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
