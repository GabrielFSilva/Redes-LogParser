using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System;

[System.Serializable]
public struct BlockInfo
{
    public int number; // Block ID
    public int parts; // Parts of the block
    public string hash; // Block Hash code

    /*------Mining Node Log------*/
    public string timeOfCreationS; // Time of the block creation. Example Format: "13:57:02.807"
    public DateTime timeOfCreation; // Converted time of creation
    public float creationElapsedTime; // Creation elapsed time (I believe it's the creation duration)

    public string timeOfSealingS; // Time of the block sealing. Example Format: "13:57:02.807"
    public DateTime timeOfSealing; // Converted time of sealing

    public string timeOfChainReachS; // Time the block reached the chain. Example Format: "13:57:02.807"
    public DateTime timeOfChainReach; // Converted time the block reached the chain
    /*---------------------------*/

    /*------Other Nodes Log------*/
    public List<string> timeOfImportS; // The the block was imported. Example Format: "13:57:02.807"
    public List<DateTime> timeOfImport; // Converted time of import
    public List<float> importElapsedTime; // Import elapsed time (I believe it's the import duration)

    public float meanImportDuration;
    /*---------------------------*/
}

public class LogReader : MonoBehaviour {

    public int minerNodeLogIndex = 1;
    public int logStartIndex = 2;
    public int fileEndIndex = 25;
    public int minValidBlockImportCount = 15;

    public List<string> commitNewMiningWorkLines = new List<string>();
    public List<string> sealedNewBlockLines = new List<string>();
    public List<string> minedPotentialBlockLines = new List<string>();
    public List<string> blockReachedCanonicalChainLines = new List<string>();

    public List<string> lines;
    public List<string> chainSegments;
    public List<BlockInfo> blocks;

    void Start()
    {
        lines = new List<string>();
        blocks = new List<BlockInfo>();

        ParseMinerLog();
        CreateBlocks();
        ParseFiles();

        ClearIncompleteBlocks();
        /*for (int j = 0; j < blocks.Count; j++)
        {
            Debug.Log(blocks[j].parts);
        }*/
    }

    private void ParseMinerLog()
    {
        TextAsset bindata = Resources.Load("ethProj/no" + minerNodeLogIndex.ToString() + "/etherLog") as TextAsset;
        lines = bindata.text.Split(new char[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries).ToList();
        chainSegments = new List<string>(lines);

        for (int i = 0; i < chainSegments.Count; i++)
        {
            if (chainSegments[i].Contains("Commit new mining work"))
            {
                commitNewMiningWorkLines.Add(chainSegments[i]);
            }
            else if (chainSegments[i].Contains("Successfully sealed new block"))
            {
                sealedNewBlockLines.Add(chainSegments[i]);
            }
            /*else if (chainSegments[i].Contains("mined potential block"))
            {
                minedPotentialBlockLines.Add(chainSegments[i]);
            }*/
            else if (chainSegments[i].Contains("block reached canonical chain"))
            {
                blockReachedCanonicalChainLines.Add(chainSegments[i]);
            }
        }
        chainSegments.Clear();
    }

    private void CreateBlocks()
    {
        List<string> lineBits = new List<string>();
        string aux;

        /*
         * Creation of new blocks
         * 
         * Log example:
         * INFO [09-12|13:57:02.807] Commit new mining work number=1 txs=0 uncles=0 elapsed=138.511µs
         */
        for (int i = 0; i < commitNewMiningWorkLines.Count; i++)
        {
            lineBits = commitNewMiningWorkLines[i].Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries).ToList();
            BlockInfo block = new BlockInfo();

            foreach (string bit in lineBits)
            {
                if (bit.StartsWith("[") && bit.EndsWith("]"))
                {
                    aux = bit.Split('|')[1];
                    aux = aux.Replace("]", "");
                    block.timeOfCreationS = aux;
                    block.timeOfCreation = StringToDateTime(aux);
                }
                else if (bit.StartsWith("number="))
                {
                    block.number = int.Parse(bit.Replace("number=", ""));
                }
                else if (bit.StartsWith("elapsed="))
                {
                    aux = bit.Replace("elapsed=", "");
                    aux = aux.Remove(aux.Length - 2);
                    if (bit.Contains('m'))
                        block.creationElapsedTime = float.Parse(aux);
                    else
                        block.creationElapsedTime = float.Parse(aux) / 1000f;
                }
                
            }
            blocks.Add(block);
        }
        /*
          * Sealing process of blocks
          * 
          * Log example:
          * INFO [09-12|13:57:04.249] Successfully sealed new block number=1 hash=38784d…5bb84d
          */
        for (int i = 0; i < sealedNewBlockLines.Count; i++)
        {
            lineBits = sealedNewBlockLines[i].Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries).ToList();
            int __blockNumber = 0;
            string __timeOfSealingS = "";
            DateTime __timeOfSealing = new DateTime();
            string __hash = "";

            foreach (string bit in lineBits)
            {
                if (bit.StartsWith("[") && bit.EndsWith("]"))
                {
                    aux = bit.Split('|')[1];
                    aux = aux.Replace("]", "");
                    __timeOfSealingS = aux;
                    __timeOfSealing = StringToDateTime(aux);
                    
                }
                else if (bit.StartsWith("number="))
                {
                    __blockNumber = int.Parse(bit.Replace("number=", ""));
                }
                else if (bit.StartsWith("hash="))
                {
                    __hash = bit.Replace("hash=", "");
                }
            }
            for(int j = 0; j< blocks.Count; j ++)
            {
                if (blocks[j].number == __blockNumber)
                {
                    BlockInfo __block = blocks[j];
                    __block.timeOfSealingS = __timeOfSealingS;
                    __block.timeOfSealing = __timeOfSealing;
                    __block.hash = __hash;
                    blocks[j] = __block;
                    break;
                }
            }
           
        }
        /*
         * Block reached the chain
         * 
         * Log example:
         * INFO [09-12|13:57:38.488] 🔗 block reached canonical chain number=26 hash=0e9e8c…2cc91c
         */
        for (int i = 0; i < blockReachedCanonicalChainLines.Count; i++)
        {
            lineBits = blockReachedCanonicalChainLines[i].Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries).ToList();
            int __blockNumber = 0;
            string __timeOfChainReachS = "";
            DateTime __timeOfChainReach = new DateTime();

            foreach (string bit in lineBits)
            {
                if (bit.StartsWith("[") && bit.EndsWith("]"))
                {
                    aux = bit.Split('|')[1];
                    aux = aux.Replace("]", "");
                    __timeOfChainReachS = aux;
                    __timeOfChainReach = StringToDateTime(aux);
                }
                else if (bit.StartsWith("number="))
                {
                    __blockNumber = int.Parse(bit.Replace("number=", ""));
                }
            }

            for (int j = 0; j < blocks.Count; j++)
            {
                if (blocks[j].number == __blockNumber)
                {
                    BlockInfo __block = blocks[j];
                    __block.timeOfChainReachS = __timeOfChainReachS;
                    __block.timeOfChainReach = StringToDateTime(__timeOfChainReachS);
                    blocks[j] = __block;
                    break;
                }
            }
        }
    }

    private void ParseFiles()
    {
        for (int i = logStartIndex; i <= fileEndIndex; i++)
            ParseFile(i);
    }

    private void ParseFile(int fileIndex)
    {
        /*
         * Imported new chain segment
         * 
         * Log example:
         * INFO [09-12|13:57:10.714] Imported new chain segment 
         *  blocks=2 txs=0 mgas=0.000 elapsed=901.128µs mgasps=0.000 
         *  number=2 hash=2432b5…30bf5e cache=424.00B
         */
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
            int __blockNumber = 0;
            string __timeOfImport = "";
            float __importElapsedTime = 0f;
            /*float __cache = 0f;
            string __hash = "";*/
            int __parts = 0;

            foreach (string bit in lineBits)
            {
                if (bit.StartsWith("blocks="))
                {
                    __parts = int.Parse(bit.Replace("blocks=", ""));
                }
                else if (bit.StartsWith("elapsed="))
                {
                    aux = bit.Replace("elapsed=", "");
                    aux = aux.Remove(aux.Length - 2);
                    if (bit.Contains('m'))
                        __importElapsedTime = float.Parse(aux);
                    else
                        __importElapsedTime = float.Parse(aux) / 1000f;
                }
                else if (bit.StartsWith("number="))
                {
                    __blockNumber = int.Parse(bit.Replace("number=", ""));
                }
                else if (bit.StartsWith("[") && bit.EndsWith("]"))
                {
                    aux = bit.Split('|')[1];
                    __timeOfImport = aux.Replace("]", "");
                }
                /*else if (bit.StartsWith("hash="))
                {
                    __hash = bit.Replace("hash=", ""); ;
                }*/
                /*else if (bit.StartsWith("cache="))
                {
                    aux = bit.Replace("cache=", "");
                    aux = aux.Remove(aux.Length - 2);
                    __cache = float.Parse(aux);
                }*/
            }
            for (int j = 0; j < blocks.Count; j++)
            {
                if (blocks[j].number == __blockNumber)
                {
                    BlockInfo __block = blocks[j];
                    if (__block.timeOfImportS == null)
                    {
                        __block.timeOfImportS = new List<string>();
                        __block.timeOfImport = new List<DateTime>();
                    }
                    __block.timeOfImportS.Add(__timeOfImport);
                    __block.timeOfImport.Add(StringToDateTime(__timeOfImport));
                    if (__block.importElapsedTime == null)
                        __block.importElapsedTime = new List<float>();
                    __block.importElapsedTime.Add(__importElapsedTime);
                    __block.parts = __parts;
                    blocks[j] = __block;
                    break;
                }
            }
        }
    }

    private void ClearIncompleteBlocks()
    {
        for (int i = blocks.Count - 1; i >= 0; i--)
        {
            if (blocks[i].number == 0 || blocks[i].hash == null || blocks[i].timeOfCreationS == null
                || blocks[i].timeOfSealingS == null /*|| blocks[i].timeOfChainReachS == null*/ || blocks[i].timeOfImportS == null
                || blocks[i].timeOfImportS.Count < minValidBlockImportCount)
                blocks.RemoveAt(i);
        }
    }

    private DateTime StringToDateTime(string text)
    {
        return new DateTime(2018, 9, 12, int.Parse(text.Split(':')[0]), int.Parse(text.Split(':')[1]), 
            int.Parse((text.Split(':')[2]).Split('.')[0]), int.Parse((text.Split(':')[2]).Split('.')[1]));
    }
}
