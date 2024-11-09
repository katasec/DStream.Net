using System;
using System.Collections.Generic;

namespace DStream.Net;

public class CDCChangeResult
{
    public bool ChangesFound { get; set; }
    public byte[] NewLSN { get; set; }
    public List<Dictionary<string, object>> ChangeDataList { get; set; }

    public CDCChangeResult(bool changesFound, byte[] newLSN, List<Dictionary<string, object>> changeDataList)
    {
        ChangesFound = changesFound;
        NewLSN = newLSN;
        ChangeDataList = changeDataList;
    }
}
