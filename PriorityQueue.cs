using System;
using System.Collections;
using System.Collections.Generic;

public class PriorityQueue<T> where T : IComparable<T>
{
    //The brains
    public List<T> info;

    //Default constructor, just makes an empty list
    public PriorityQueue()
    {
        this.info = new List<T>();
    }

    //Insert items into the Queue, smaller info = higher priority
    public void Enqueue(T item)
    {
        info.Add(item);
        int childIndex = info.Count - 1;
        while (childIndex > 0)
        {
            int parentIndex = (childIndex - 1)/ 2;
            if (info[childIndex].CompareTo(info[parentIndex]) >= 0)
                break;
            T tmp = info[childIndex];
            info[childIndex] = info[parentIndex];
            info[parentIndex] = tmp;
            childIndex = parentIndex;
        }
    }

    //Dequeue from the top
    public T Dequeue()
    {
        int lastIndex = info.Count - 1;
        T frontItem = info[0];
        info[0] = info[lastIndex];
        info.RemoveAt(lastIndex);

        --lastIndex;
        int parentIndex = 0;
        while (true)
        {
            int childIndex = parentIndex * 2 + 1;
            if (childIndex > lastIndex)
                break;
            int rightChild = childIndex + 1;
            if (rightChild <= lastIndex && info[rightChild].CompareTo(info[childIndex]) < 0)
                childIndex = rightChild;
            if (info[parentIndex].CompareTo(info[childIndex]) <= 0)
                break;
            T tempItem = info[parentIndex];
            info[parentIndex] = info[childIndex];
            info[childIndex] = tempItem;
            parentIndex = childIndex;
        }
        return frontItem;
    }

    //Return the top without Dequeueing
    public T Peek()
    {
        T frontItem = info[0];
        return frontItem;
    }

    //Returns the count
    public int Count()
    {
        return info.Count;
    }

    //prints it if you want to Debug.Log ;)
    public override string ToString()
    {
        string s = "";
        for (int i = 0; i < info.Count; ++i)
            s += info[i].ToString() + " ";
        s += "count = " + info.Count;
        return s;
    }
} // PriorityQueue
