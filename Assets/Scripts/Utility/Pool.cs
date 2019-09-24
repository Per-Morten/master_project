using System.Collections.Generic;
using UnityEngine;

public class Pool
{
    public Pool(GameObject type, int blockSize = 2048)
    {
        mTemplate = Object.Instantiate(type, type.transform.parent) as GameObject;
        mTemplate.SetActive(false);
        mFree = new Stack<GameObject>();
        mBlockSize = blockSize;
        AddBlock();
    }

    public GameObject CreateObject()
    {
        if (mFree.Count == 0)
            AddBlock();

        var go = mFree.Pop();
        go.SetActive(true);
        return go;
    }

    public void DestroyObject(GameObject go)
    {
        go.SetActive(false);
        mFree.Push(go);
    }

    public void AddBlock()
    {
        for (int i = 0; i < mBlockSize; i++)
        {
            GameObject item = Object.Instantiate(mTemplate, Vector3.zero, Quaternion.identity, mTemplate.transform.parent) as GameObject;
            item.name = mTemplate.name;
            item.SetActive(false);
            mFree.Push(item);
        }
    }

    private GameObject mTemplate;
    private int mBlockSize;
    private Stack<GameObject> mFree;
}
