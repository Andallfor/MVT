using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public abstract class body : IBody
{
    public string name {get; protected set;}
    public position pos {get; protected set;}
    public position localPos {get; protected set;}
    public event EventHandler onPositionUpdate = delegate {};

    private protected body parent = null;
    private protected List<body> children = new List<body>();

    public static void addFamilyNode(body parent, body child)
    {
        master.time.onChange -= parent.updatePosition;
        master.time.onChange -= child.updatePosition;
        parent.onPositionUpdate -= child.updatePosition;
        child.parent = parent;
        parent.children.Add(child);

        master.time.onChange += parent.updatePosition;
        parent.onPositionUpdate += child.updatePosition;
    }
    public static void removeFamilyNode(body child)
    {
        master.time.onChange -= child.updatePosition;
        child.parent.children.Remove(child);

        master.time.onChange += child.updatePosition;
        child.parent.onPositionUpdate -= child.updatePosition;

        child.parent = null;
    }

    public jsonBodyStruct requestJsonFile()
    {
        List<string> d = new List<string>();
        foreach (planet p in children)
        {
            d.Add(p.name);
        }

        return new jsonBodyStruct() {
            parent = (ReferenceEquals(parent, null)) ? "" : parent.name,
            children = d.ToArray()};
    }

    public abstract void updatePosition(object sender, EventArgs args);
    public abstract void updateScale(object sender, EventArgs args);
    public abstract position requestLocalPosition(Time t);
    protected void updateChildren() {onPositionUpdate(null, EventArgs.Empty);}

    private protected abstract void loadPhysicalData(representationData rData);

    private protected void init(representationData rData)
    {
        loadPhysicalData(rData);
        master.updatePositions += updatePosition;
        master.time.onChange += updatePosition;
        master.onScaleChange += updateScale;
    }

    public override int GetHashCode() => name.GetHashCode();
}

public interface IBody
{
    
}