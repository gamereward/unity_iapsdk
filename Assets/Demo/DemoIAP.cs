using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DemoIAP : MonoBehaviour
{
    public GameObject requestPanel;
    public GameObject itemTemplate;
    private string[] itemCodes = { "gamercoinpack", "supercoinpack" };
    private Grd.GrdItem[] items;
    private Grd.GrdPurchaseRequest request;
    // Use this for initialization
    void Start()
    {
        requestPanel.gameObject.SetActive(false);
        itemTemplate.gameObject.SetActive(false);
        Grd.GrdManager.Init("a9d42958088fd0cbc093378285852b8b71f3f45e", "8c6744c9d42ec2cb9e8885b54ff744d094f6d7e04a4d452035300f18b984988cc52f1bd66cc19d05628bd8bf27af3ad61d13fd714139ea639d44f41fb13ebf65", Grd.GrdNet.Test);
        LoadItemsInfo();
    }
    private IEnumerator DownLoadIcon(string url, Image img)
    {
        if (url != null && url.Length > 0)
        {
            WWW w = new WWW(url);
            yield return w;
            if (w.texture != null)
            {
                img.sprite = Sprite.Create(w.texture, new Rect(0, 0, (int)w.texture.width, (int)w.texture.height), new Vector2(0.5f, 0.5f));
            }
        }
        else
        {
            img.gameObject.SetActive(false);
        }
    }
    public void OnCloseRequestPanel()
    {
        requestPanel.SetActive(false);
    }
    private void ShowBuy(int itemIndex)
    {
        Grd.GrdManager.RequestBuyItem(items[itemIndex].itemcode, 1, (error, args) =>
        {
            if (error == 0)
            {
                allowChecking = true;
                request = args.Data;
                requestPanel.SetActive(true);
                requestPanel.transform.GetChild(0).Find("ItemName").GetComponent<Text>().text = items[itemIndex].itemname;
                requestPanel.transform.GetChild(0).Find("Price").GetComponent<Text>().text = items[itemIndex].price.ToString();
                requestPanel.transform.GetChild(0).Find("RequestStatus").GetComponent<Text>().text = "Waiting for purchase...";
                Grd.GrdManager.GetRequestPurchaseQRCode(request.RequestId, (err1, args1) =>
                {
                    if (err1 == 0)
                    {
                        var qrcode = args1.Data;
                        requestPanel.transform.GetChild(0).Find("QRCode").GetComponent<Image>().sprite = Sprite.Create(qrcode, new Rect(0, 0, qrcode.width, qrcode.height), new Vector2(0.5f, 0.5f));
                    }
                });
                StartCoroutine(DownLoadIcon(items[itemIndex].itemicon, requestPanel.transform.GetChild(0).Find("ItemIcon").GetComponent<Image>()));
            }
        });
    }
    private void LoadItemsInfo()
    {
        Grd.GrdManager.GetItemsInfo(itemCodes, (error, args) =>
        {
            if (error == 0)
            {
                items = args.Data;
                for (int i = 0; i < args.Data.Length; i++)
                {
                    GameObject item = Instantiate(itemTemplate, itemTemplate.transform.parent);
                    item.SetActive(true);
                    StartCoroutine(DownLoadIcon(args.Data[i].itemicon, item.transform.Find("ItemIcon").GetComponent<Image>()));
                    item.transform.Find("ItemName").GetComponent<Text>().text = args.Data[i].itemname;
                    item.transform.Find("Price").GetComponent<Text>().text = args.Data[i].price.ToString();
                    var itemIndex = i;
                    item.transform.Find("Buy").GetComponent<Button>().onClick.AddListener(() =>
                    {
                        ShowBuy(itemIndex);
                    });
                }
            }
        });
    }
    private bool allowChecking = false;
    private float waitTime = 0;
    // Update is called once per frame
    void Update()
    {
        if (requestPanel.activeSelf)
        {
            if (!allowChecking)
            {
                return;
            }
            if (waitTime > 0)
            {
                waitTime -= Time.deltaTime;
                return;
            }
            Grd.GrdManager.CheckItemStatus(request.RequestId, (error, args) =>
            {
                if (error == 0)
                {
                    if (args.Data != Grd.GrdPurchaseStatus.NewRequest && args.Data != Grd.GrdPurchaseStatus.Pending)
                    {
                        requestPanel.transform.GetChild(0).Find("RequestStatus").GetComponent<Text>().text = args.Data.ToString();
                        allowChecking = false;
                    }
                    else
                    {
                        waitTime = 2;//wait 2s
                        allowChecking = true;
                    }
                }
            });
        }
    }
}
