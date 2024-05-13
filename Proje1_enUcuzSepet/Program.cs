using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;

class Program
{
    static HttpClient httpClient = new HttpClient();

    static async Task<List<Tuple<string, double>>> UrunAraAsyncSokMarket(string urunAdi)
    {
        string formatliUrunAdi = urunAdi.Replace(" ", "%20");
        string url = $"https://www.sokmarket.com.tr/arama?q={formatliUrunAdi}";

        string htmlIcerik = await httpClient.GetStringAsync(url);

        var doc = new HtmlDocument();
        doc.LoadHtml(htmlIcerik);

        var urunDugumleri = doc.DocumentNode.SelectNodes("//div[contains(@class,'PLPProductListing_PLPCardParent__GC2qb')]");

        List<Tuple<string, double>> urunler = new List<Tuple<string, double>>();

        if (urunDugumleri != null)
        {
            await Task.WhenAll(urunDugumleri.Select(async urunDugumu =>
            {
                var urunBilgiDugumu = urunDugumu.SelectSingleNode(".//a");

                if (urunBilgiDugumu == null)
                    return;

                string urunBasligi = urunBilgiDugumu.SelectSingleNode(".//h2[contains(@class,'CProductCard-module_title__u8bMW')]").InnerText;

                urunBasligi = urunBasligi.ToLower();
                urunAdi = urunAdi.ToLower();

                string[] urunBas = urunBasligi.Split();
                string[] urunAd = urunAdi.Split();

                int kontrol = 0;
                for (int i = 0; i < urunBas.Length; i++)
                {
                    for (int j = 0; j < urunAd.Length; j++)
                    {
                        if (urunBas[i] == urunAd[j])
                        {
                            kontrol++;
                        }
                        else
                        {

                        }
                    }

                }
                if (kontrol != urunAd.Length)
                {
                    return;
                }

                string urunFiyatXPath = ".//span[contains(@class,'CPriceBox-module_price__bYk-c')]";
                var urunFiyatDugumu = urunBilgiDugumu.SelectSingleNode(urunFiyatXPath);

                if (urunFiyatDugumu != null)
                {
                    string urunFiyatMetni = urunFiyatDugumu.InnerText;
                    double urunFiyati = FiyatCikar(urunFiyatMetni);
                    urunler.Add(new Tuple<string, double>(urunBasligi, urunFiyati));
                }
            }));
        }

        return urunler;
    }

    static async Task<List<Tuple<string, double>>> UrunAraAsyncCarrefour(string urunAdi)
    {
        string formatliUrunAdi = urunAdi.Replace(' ', '-');
        string url = $"https://www.carrefoursa.com/search/?text={formatliUrunAdi}";

        string htmlIcerik = await httpClient.GetStringAsync(url);

        var doc = new HtmlDocument();
        doc.LoadHtml(htmlIcerik);

        var urunDugumleri = doc.DocumentNode.SelectNodes("//li[contains(@class,'product-listing-item')]");

        List<Tuple<string, double>> urunler = new List<Tuple<string, double>>();

        if (urunDugumleri != null)
        {
            await Task.WhenAll(urunDugumleri.Select(async urunDugumu =>
            {
                var urunBilgiDugumu = urunDugumu.SelectSingleNode(".//a[@href]");

                if (urunBilgiDugumu == null)
                    return;

                string urunBasligi = urunBilgiDugumu.SelectSingleNode(".//span[contains(@class,'item-name')]").InnerText;

                urunBasligi = urunBasligi.ToLower();
                urunAdi = urunAdi.ToLower();

                string[] urunBass = urunBasligi.Split();
                string[] urunAdd = urunAdi.Split();

                int kontrol = 0;
                for (int i = 0; i < urunBass.Length; i++)
                {
                    for (int j = 0; j < urunAdd.Length; j++)
                    {
                        if (urunBass[i] == urunAdd[j])
                        {
                            kontrol++;
                        }
                        else
                        {

                        }
                    }

                }
                if (kontrol != urunAdd.Length)
                {
                    return;
                }

                string urunFiyatXPath = ".//span[contains(@class,'item-price')]/@content";
                var urunFiyatDugumu = urunBilgiDugumu.SelectSingleNode(urunFiyatXPath);

                if (urunFiyatDugumu != null)
                {
                    string urunFiyatMetni = urunFiyatDugumu.GetAttributeValue("content", "");
                    double urunFiyati = FiyatCikar(urunFiyatMetni);
                    urunler.Add(new Tuple<string, double>(urunBasligi, urunFiyati));
                }
            }));
        }

        return urunler;
    }

    static object klit = new object();
    static double FiyatCikar(string fiyatMetni)
    {
        lock (klit)
        {
            string[] parcalar = fiyatMetni.Split(new char[] { ' ', ',', '.' }, StringSplitOptions.RemoveEmptyEntries);

            if (double.TryParse(parcalar[0], out double fiyat))
            {
                return fiyat;
            }
            else
            {
                return 0.0;
            }
        }
    }

    static async Task Main(string[] args)
    {
        Console.WriteLine("Lütfen ürün isimlerini virgülle ayırarak girin:");
        string[] urunAdlari = Console.ReadLine().Split(',');

        urunAdlari = urunAdlari.Select(ad => ad.Trim()).ToArray();

        Dictionary<string, Tuple<string, double>> enUcuzUrunlerSokMarket = new Dictionary<string, Tuple<string, double>>();
        Dictionary<string, Tuple<string, double>> enUcuzUrunlerCarrefour = new Dictionary<string, Tuple<string, double>>();

        await Task.WhenAll(urunAdlari.Select(async urunAdi =>
        {
            var urunlerSokMarket = await UrunAraAsyncSokMarket(urunAdi);
            var urunlerCarrefour = await UrunAraAsyncCarrefour(urunAdi);

            if (urunlerSokMarket.Count > 0 && urunlerCarrefour.Count > 0)
            {
                var enUcuzUrunSokMarket = urunlerSokMarket.OrderBy(urun => urun.Item2).First();
                var enUcuzUrunCarrefour = urunlerCarrefour.OrderBy(urun => urun.Item2).First();

                enUcuzUrunlerSokMarket.Add(urunAdi, enUcuzUrunSokMarket);
                enUcuzUrunlerCarrefour.Add(urunAdi, enUcuzUrunCarrefour);
            }
        }));

        double toplamFiyatSokMarket = enUcuzUrunlerSokMarket.Sum(urun => urun.Value.Item2);
        double toplamFiyatCarrefour = enUcuzUrunlerCarrefour.Sum(urun => urun.Value.Item2);

        Console.WriteLine("Ürünler ve fiyatları:");
        foreach (var urun in enUcuzUrunlerSokMarket)
        {
            Console.WriteLine($"Şok Market - {urun.Key}: {urun.Value.Item1}, Fiyatı: {urun.Value.Item2} TL");
        }
        foreach (var urun in enUcuzUrunlerCarrefour)
        {
            Console.WriteLine($"Carrefour - {urun.Key}: {urun.Value.Item1}, Fiyatı: {urun.Value.Item2} TL");
        }
        Console.WriteLine($"Toplam fiyat Şok Market: {toplamFiyatSokMarket} TL");
        Console.WriteLine($"Toplam fiyat Carrefour: {toplamFiyatCarrefour} TL");

        double enUcuzToplamFiyat = Math.Min(toplamFiyatSokMarket, toplamFiyatCarrefour);
        Console.WriteLine($"En ucuz toplam fiyat: {enUcuzToplamFiyat} TL");

        Console.ReadLine();
    }
}
