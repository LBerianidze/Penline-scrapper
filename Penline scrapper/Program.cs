using HtmlAgilityPack;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace Penline_scrapper
{
	internal class Program
	{
		private static void GetAllNodes(HtmlNode node, List<HtmlNode> nodes)
		{
			nodes.Add(node);
			foreach (var item in node.ChildNodes)
			{
				GetAllNodes(item, nodes);
			}

		}

		private static HtmlNode GetElementById(List<HtmlNode> nodes, string id)
		{
			foreach (var item in nodes)
			{
				if (item.Id == id)
				{
					return item;
				}
			}
			return null;
		}

		private static HtmlNode GetElementWithAttribute(List<HtmlNode> nodes, string elementName, string attribute)
		{
			foreach (var item in nodes)
			{
				if (item.Name == elementName && item.Attributes.FirstOrDefault(e => e.Name == attribute) != null)
				{
					return item;
				}
			}
			return null;
		}

		private static HtmlNode GetElementWithAttributeValue(List<HtmlNode> nodes, string elementName, string attribute, string attributeValue)
		{
			foreach (var item in nodes)
			{
				if (item.Name == elementName && item.Attributes.FirstOrDefault(e => e.Name == attribute) != null)
				{
					if (item.Attributes.FirstOrDefault(e => e.Name == attribute).Value == attributeValue)
					{
						return item;
					}
				}
			}
			return null;
		}
		private static string GetPageSource()
		{
			var request = new xNet.HttpRequest("https://happy-number.ru/Tj6S2ox5Kb/")
			{
				EnableEncodingContent = true,
				Cookies = new xNet.CookieDictionary(false)
			};


			request.AddHeader("Accept", "*/*");
			request.AddHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/85.0.4183.83 Safari/537.36");
			request.AddHeader("Accept-Language", "ru-RU,ru;q=0.8,en-US;q=0.6,en;q=0.4,zh-CN;q=0.2,zh;q=0.2,sv;q=0.2,zh-TW;q=0.2,es;q=0.2,de;q=0.2,nl;q=0.2");
			request.AddHeader("X-Requested-With", "XMLHttpRequest");

			var resp = request.Get("https://capitalgift.ru/category/catalog/?page={++currentPage}").ToString();
			HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
			doc.LoadHtml(resp);
			List<HtmlNode> allNodes = new List<HtmlNode>();
			GetAllNodes(doc.DocumentNode, allNodes);
			var productList = GetElementById(allNodes, "product-list").ChildNodes[1];
			return resp;
		}
		static Random random = new Random();
		static string Directory = AppDomain.CurrentDomain.BaseDirectory + "images\\";

		private static void Main12(string[] args)
		{
			int currentPage = 1;
			OpenQA.Selenium.Chrome.ChromeDriver chrome = new OpenQA.Selenium.Chrome.ChromeDriver
			{
				Url = "https://capitalgift.ru/category/catalog/?page=1"
			};
			List<PenlineProduct> penlineProducts = new List<PenlineProduct>();
			int amount = 0;
			while (!chrome.PageSource.Contains("Не найдено ни одного товара."))
			{
				HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
				doc.LoadHtml(chrome.PageSource);
				List<HtmlNode> allNodes = new List<HtmlNode>();
				GetAllNodes(doc.DocumentNode, allNodes);
				var productList = GetElementById(allNodes, "product-list").ChildNodes[1];
				foreach (var item in productList.ChildNodes)
				{
					Console.Clear();
					Console.WriteLine("Page: " + currentPage);

					PenlineProduct product = new PenlineProduct();
					List<HtmlNode> ilnodes = new List<HtmlNode>();
					GetAllNodes(item, ilnodes);
					HtmlNode aNode = GetElementWithAttribute(ilnodes, "a", "href");
					string url = "https://capitalgift.ru" + aNode.Attributes["href"].Value.Replace("&amp;", "&");
					product.URL = url;
					chrome.Url = url;
					doc = new HtmlAgilityPack.HtmlDocument();
					doc.LoadHtml(chrome.PageSource);
					if (chrome.PageSource.Contains("Такой страницы не существует") || chrome.PageSource.Contains("Внутренняя ошибка"))
						continue;

					List<HtmlNode> productNodes = new List<HtmlNode>();
					GetAllNodes(doc.DocumentNode, productNodes);
					var header = GetElementById(productNodes, "product-features")?.ChildNodes.ElementAt(0);
					product.Articul = GetElementWithAttributeValue(productNodes, "div", "class", "article-block").ChildNodes[0].InnerText.Split(':')[1].Trim();
					product.Name = GetElementWithAttributeValue(productNodes, "span", "class", "product-title").InnerText;
					product.Description = HttpUtility.HtmlDecode(GetElementWithAttributeValue(productNodes, "div", "class", "product-description-text")?.InnerText);
					product.Price = GetElementWithAttributeValue(productNodes, "div", "class", "price-block")?.ChildNodes[0].ChildNodes[0].InnerText;

					foreach (var item2 in header.ChildNodes)
					{
						if (item2.Attributes.Contains("class"))
						{
							product.Characteristics.Add(item2.ChildNodes[0].InnerText.Split(':')[0], item2.ChildNodes[1].InnerText);
						}
					}
					var images = GetElementById(productNodes, "product-gallery");
					if (images != null || images.ChildNodes.Count == 1)
					{
						foreach (var item2 in images.ChildNodes)
						{
							if (item2.Name == "div")
							{
								var imageUrl = "https://capitalgift.ru" + item2.ChildNodes[1].Attributes["data-image"].Value;
								product.Images.Add(imageUrl);
							}
						}
					}
					else
					{
						var imagediv = GetElementById(productNodes, "product-image-clone");
						if (imagediv != null)
						{
							var imageUrl = "https://capitalgift.ru" + imagediv.ChildNodes[0].Attributes["src"].Value;
							product.Images.Add(imageUrl);
						}
						else
						{
							imagediv = GetElementById(productNodes, "product-core-image");
							if (imagediv != null)
							{
								var imageUrl = "https://capitalgift.ru" + imagediv.ChildNodes[5].ChildNodes[1].ChildNodes[0].Attributes["src"].Value;
								product.Images.Add(imageUrl);
							}
							else
							{
								string style = GetElementWithAttributeValue(productNodes, "div", "class", "zoomLens")?.Attributes["style"]?.Value;
								string pattern = @"url\(""(.+?)""\)";
								var matches = Regex.Matches(style, pattern, RegexOptions.Multiline);
								if (matches.Count != 0)
								{
									var imageUrl = "https://capitalgift.ru" + matches[1].Value;
									product.Images.Add(imageUrl);

								}
							}
						}
					}
					WebClient wb = new WebClient();

					for (int i = 0; i < product.Images.Count; i++)
					{
						string filename = product.Images[i].Split('/').Last();
						while (File.Exists(Directory + filename))
						{
							filename = random.Next(0, 9) + filename;
						}
						try
						{
							string path = Directory + filename;
							wb.DownloadFile(product.Images[i], path);
							product.Images[i] = "images\\" + filename;
						}
						catch (WebException ex)
						{
							if (ex.Message != "The remote server returned an error: (404) Not Found.")
							{

							}
						}

					}
					if (!product.Characteristics.ContainsKey("Подкатегория"))
					{
						if (product.Characteristics.ContainsKey("Тип зажигалки"))
						{
							product.Category = "Зажигалки";
						}
						else
						{
							Console.WriteLine(product.URL);
						}
					}
					else
					{
						product.Category = product.Characteristics["Подкатегория"];
					}
					penlineProducts.Add(product);
					amount++;
					Console.WriteLine(amount + " on page " + currentPage);
				}
				chrome.Url = $"https://capitalgift.ru/category/catalog/?page={++currentPage}";
			}
			XDocument xml_document = new XDocument();
			XElement products_node = new XElement("Products");
			xml_document.Add(products_node);
			foreach (var item in penlineProducts)
			{
				var product_node = new XElement("Product");
				product_node.Add(new XElement("Name", item.Name));
				product_node.Add(new XElement("URL", item.URL));
				product_node.Add(new XElement("Category", item.Category));
				product_node.Add(new XElement("Articul", item.Articul));
				product_node.Add(new XElement("Price", item.Price));
				product_node.Add(new XElement("Description", item.Description));
				var characteristics = new XElement("Characteristics");
				foreach (var characteristic in item.Characteristics)
				{
					characteristics.Add(new XElement(System.Xml.XmlConvert.EncodeName(characteristic.Key), characteristic.Value));

				}
				product_node.Add(characteristics);
				var images = new XElement("Images");
				foreach (var image in item.Images)
				{
					images.Add(new XElement("Image", image));

				}
				product_node.Add(images);
				products_node.Add(product_node);

			}
			xml_document.Save(AppDomain.CurrentDomain.BaseDirectory + "result.xml");
			chrome.Quit();
		}
		private static void Main()
		{
			string txt = File.ReadAllText(@"C:\Users\User\source\repos\Penline scrapper\Penline scrapper\bin\Debug\result.xml");
			XmlDocument doc = new XmlDocument();
			doc.LoadXml(txt);
			List<PenlineProduct> prlist = new List<PenlineProduct>();
			dynamic jsonText = JsonConvert.DeserializeObject(JsonConvert.SerializeXmlNode(doc));
			dynamic products = jsonText.Products;
			foreach (var item in products.Product)
			{
				PenlineProduct pr = new PenlineProduct();
				pr.URL = item.URL;
				pr.Category = item.Category;
				pr.Name = item.Name;
				pr.Articul = item.Articul;
				foreach (var cha in item.Characteristics)
				{
					pr.Characteristics.Add(cha.Name.ToString(), cha.Value.ToString());
				}
				pr.Description = item.Description;
				pr.Price = item.Price;
				if (item.Images != null)
					foreach (var cha in item.Images)
					{
						foreach (var ch in cha)
						{
							if (ch.Type.ToString() == "Array")
							{
								foreach (var c in ch)
								{
									pr.Images.Add(c.ToString());
								}
							}
							else
							{
								pr.Images.Add(ch.ToString());
							}
						}
					}
				prlist.Add(pr);
			}
			var fff = prlist.Where(t => t.Images.Count > 1);

			XDocument xml_document = new XDocument();
			XElement products_node = new XElement("Products");
			xml_document.Add(products_node);
			foreach (var item in prlist)
			{
				var product_node = new XElement("Product");
				product_node.Add(new XElement("Name", item.Name));
				product_node.Add(new XElement("URL", item.URL));
				product_node.Add(new XElement("Category", item.Category));
				product_node.Add(new XElement("Articul", item.Articul));
				product_node.Add(new XElement("Price", item.Price));
				product_node.Add(new XElement("Description", item.Description));
				var characteristics = new XElement("Characteristics");
				foreach (var characteristic in item.Characteristics)
				{
					characteristics.Add(new XElement(System.Xml.XmlConvert.EncodeName(characteristic.Key), characteristic.Value));

				}
				product_node.Add(characteristics);
				int imgIndex = 1;
				foreach (var image in item.Images)
				{
					product_node.Add(new XElement("Image_"+imgIndex++, image));

				}
				products_node.Add(product_node);

			}
			xml_document.Save(AppDomain.CurrentDomain.BaseDirectory + "newresult.xml");
		}
	}

	public class PenlineProduct
	{
		public string URL { get; set; }
		public string Name { get; set; }
		public string Category { get; set; }
		public string Articul { get; set; }
		public Dictionary<string, string> Characteristics { get; set; } = new Dictionary<string, string>();
		public string Description { get; set; }
		public List<string> Images { get; set; } = new List<string>();
		public string Price { get; set; }
		public PenlineProduct()
		{

		}
	}
}
