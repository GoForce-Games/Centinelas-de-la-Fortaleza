using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Copied with slight tweaks from https://stackoverflow.com/questions/5852863/fixed-size-queue-which-automatically-dequeues-old-values-upon-new-enqueues
public class FixedSizedQueue<T> : Queue<T>
{
    private object lockObject = new object();

    public int Limit { get; set; }

    public FixedSizedQueue(int limit)
    {
        this.Limit = limit;
    }
    
    public new void Enqueue(T obj)
    {
        base.Enqueue(obj);
        lock (lockObject)
        {
            while (this.Count > Limit && this.TryDequeue(out _)) ;
        }
    }
}