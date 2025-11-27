using System;
using UnityEngine.UI;

public interface IDropUnit {
    public int UID { get; }
    public Toggle Toggle { get; }
    public Action<int> OnSelect { get; }
}