using System.Text;
using UnityEngine;
using TMPro;

public class BasketUI : MonoBehaviour
{
    public Basket basket;
    public TMP_Text headerLine;  // e.g. "Basket: 3 items — $1500"
    public TMP_Text listText;    // optional detailed list

    void OnEnable()       { if (basket) basket.onChanged.AddListener(Refresh); Refresh(); }
    void OnDisable()      { if (basket) basket.onChanged.RemoveListener(Refresh); }

    public void Refresh()
    {
        if (!basket) return;
        int count = basket.items.Count;
        int total = basket.Total;
        if (headerLine) headerLine.text = $"Basket: {count} item{(count==1?"":"s")} — ${total}";

        if (listText)
        {
            var sb = new StringBuilder();
            foreach (var it in basket.items) sb.AppendLine($"{it.name}  (${it.price})");
            listText.text = sb.ToString();
        }
    }
}