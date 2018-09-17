using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct BlockInfo
{
    public int parts;
    public float elapsedTime;
    public int number;
    public string hash;
    public float cache;
}

public class LogReader : MonoBehaviour {

    public int logStartIndex = 2;
    public int fileEndIndex = 25;

    public List<string> lines;
    public List<string> chainSegments;
    public List<BlockInfo> blocks;
	
	void Start ()
    {
        lines = new List<string>();
        blocks = new List<BlockInfo>();
        ParseFiles();
    }

    private void ParseFiles()
    {
        for (int i = logStartIndex; i <= fileEndIndex; i++)
            ParseFile(i);
    }

    private void ParseFile(int fileIndex)
    {
        TextAsset bindata = Resources.Load("ethProj/no" + fileIndex.ToString() + "/etherLog") as TextAsset;
        lines = bindata.text.Split(new char[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries).ToList();
        chainSegments = new List<string>(lines);

        for (int i = chainSegments.Count - 1; i >= 0; i--)
        {
            if (!chainSegments[i].Contains("Imported new chain segment"))
            {
                chainSegments.RemoveAt(i);
            }
        }

        for (int i = 0; i < chainSegments.Count; i++)
        {
            List<string> lineBits = chainSegments[i].Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries).ToList();
            string aux;
            BlockInfo block = new BlockInfo();

            foreach (string bit in lineBits)
            {
                if (bit.StartsWith("blocks="))
                {
                    block.parts = int.Parse(bit.Replace("blocks=", ""));
                }
                else if (bit.StartsWith("elapsed="))
                {
                    aux = bit.Replace("elapsed=", "");
                    aux = aux.Remove(aux.Length - 2);
                    if (bit.Contains('m'))
                        block.elapsedTime = float.Parse(aux);
                    else
                        block.elapsedTime = float.Parse(aux) / 1000f;
                }
                else if (bit.StartsWith("number="))
                {
                    block.number = int.Parse(bit.Replace("number=", ""));
                }
                else if (bit.StartsWith("hash="))
                {
                    block.hash = bit.Replace("hash=", ""); ;
                }
                else if (bit.StartsWith("cache="))
                {
                    aux = bit.Replace("cache=", "");
                    aux = aux.Remove(aux.Length - 2);
                    block.cache = float.Parse(aux);
                }
            }

            //Parse


            blocks.Add(block);
        }
    }
	// Update is called once per frame
	void Update () {
		
	}
}
