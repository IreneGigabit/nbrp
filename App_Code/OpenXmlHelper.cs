﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Data.SqlClient;
using System.Web;
using System.IO;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;
using System.Drawing;

/// <summary>
/// Docx 操作類別(use OpenXml SDK)
/// TODO:
/// 紙張邊界
/// 表格操作
/// </summary>
public class OpenXmlHelper {
	protected WordprocessingDocument outDoc = null;
	protected MemoryStream outMem = new MemoryStream();
	protected Body outBody = null;
	Dictionary<string, WordprocessingDocument> tplDoc = new Dictionary<string, WordprocessingDocument>();
	protected string defTplDocName = "";
	Dictionary<string, MemoryStream> tplMem = new Dictionary<string, MemoryStream>();

	public OpenXmlHelper() {
	}

	#region 關閉
	/// <summary>
	/// 關閉
	/// </summary>
	public void Dispose() {
		if (this.outDoc != null) outDoc.Dispose();
		if (this.outMem != null) outMem.Close();

		foreach (var item in tplDoc) {
			item.Value.Dispose();
		}
		foreach (var item in tplMem) {
			item.Value.Close();
			item.Value.Dispose();
		}
		//微軟KB 312629https://support.microsoft.com/en-us/help/312629/prb-threadabortexception-occurs-if-you-use-response-end--response-redi
		//Response.End、Server.Transfer、Response.Redirect被呼叫時，會觸發ThreadAbortException，因此要改用CompleteRequest()
		//HttpContext.Current.Response.End();
		HttpContext.Current.ApplicationInstance.CompleteRequest();
	}
	#endregion

	#region 建立空白檔案
	/// <summary>
	/// 建立空白檔案
	/// </summary>
	public void Create() {
		outMem = new MemoryStream();
		outDoc = WordprocessingDocument.Create(outMem, WordprocessingDocumentType.Document);
		MainDocumentPart mainPart = outDoc.AddMainDocumentPart();
		mainPart.Document = new Document();
		outBody = mainPart.Document.AppendChild(new Body());
	}
	#endregion

	#region 複製範本檔
	/// <summary>
	/// 複製範本檔
	/// </summary>
	/// <param name="templateList">範本＜別名,檔名(實體路徑)＞</param>
	public void CloneFromFile(Dictionary<string, string> templateList, bool cleanFlag) {
		foreach (var x in templateList.Select((Entry, Index) => new { Entry, Index })) {
			if (x.Index == 0) {
				byte[] outArray = File.ReadAllBytes(x.Entry.Value);
				outMem.Write(outArray, 0, (int)outArray.Length);
				outDoc = WordprocessingDocument.Open(outMem, true);
				defTplDocName = x.Entry.Key;
			}

			byte[] tplArray = File.ReadAllBytes(x.Entry.Value);
			tplMem.Add(x.Entry.Key, new MemoryStream());
			tplMem[x.Entry.Key].Write(tplArray, 0, (int)tplArray.Length);
			tplDoc.Add(x.Entry.Key, WordprocessingDocument.Open(tplMem[x.Entry.Key], false));
		}

		//清空輸出檔內容
		if (cleanFlag) {
			//outDoc.MainDocumentPart.Document.Body.RemoveAllChildren<SdtElement>();
			//outDoc.MainDocumentPart.Document.Body.RemoveAllChildren<Paragraph>();
			//outDoc.MainDocumentPart.Document.Body.RemoveAllChildren<SectionProperties>();
			outDoc.MainDocumentPart.Document.Body.RemoveAllChildren();
		}

		outBody = outDoc.MainDocumentPart.Document.Body;
	}
	#endregion

	#region 輸出檔案(memory)
	/// <summary>
	/// 輸出檔案(memory)
	/// </summary>
	public void Flush(string outputName) {
		outDoc.MainDocumentPart.Document.Save();
		outDoc.Close();
		//byte[] byteArray = outMem.ToArray();
		HttpContext.Current.Response.Clear();
		HttpContext.Current.Response.HeaderEncoding = System.Text.Encoding.GetEncoding("big5");
		HttpContext.Current.Response.AddHeader("Content-Disposition", "attachment; filename=\"" + outputName + "\"");
		HttpContext.Current.Response.ContentType = "application/octet-stream";
		HttpContext.Current.Response.AddHeader("Content-Length", outMem.Length.ToString());
		HttpContext.Current.Response.BinaryWrite(outMem.ToArray());
		//this.Dispose();
	}
	#endregion

	#region 另存檔案
	/// <summary>
	/// 另存檔案
	/// </summary>
	public void SaveTo(string outputPath) {
		outDoc.MainDocumentPart.Document.Save();
		outDoc.Close();
		using (FileStream fileStream = new FileStream(outputPath, FileMode.Create)) {
			outMem.Position = 0;
			outMem.WriteTo(fileStream);
		}
		this.Dispose();
	}
	#endregion
	
	#region 複製範本Block,回傳List
	/// <summary>
	/// 複製範本Block,回傳List
	/// </summary>
	public List<OpenXmlElement> CopyBlockList(string blockName) {
		return CopyBlockList(defTplDocName, blockName);
	}

	/// <summary>
	/// 複製範本Block,回傳List
	/// </summary>
	/// <param name="srcDocName">來源範本別名</param>
	public List<OpenXmlElement> CopyBlockList(string srcDocName, string blockName) {
		try {
			WordprocessingDocument srcDoc = tplDoc[srcDocName]; 
			List<OpenXmlElement> arrElement = new List<OpenXmlElement>();
			Tag elementTag = srcDoc.MainDocumentPart.RootElement.Descendants<Tag>()
			.Where(
				element => element.Val.Value.ToLower() == blockName.ToLower()
			).SingleOrDefault();
	
			if (elementTag != null) {
				SdtElement block = (SdtElement)elementTag.Parent.Parent;
				SdtContentBlock blockCont = block.Descendants<SdtContentBlock>().FirstOrDefault();
				if (blockCont != null) {
					IEnumerable<OpenXmlElement> childs = blockCont.ChildElements;
					foreach (var item in childs) {
						arrElement.Add(item.CloneNode(true));
					}
				}
			}
			return arrElement;
		}
		catch (Exception ex) {
			throw new Exception("複製範本Block!!(" + blockName + ")", ex);
		}
	}
	#endregion
	
	#region 複製範本Block
	/// <summary>
	/// 複製範本Block
	/// </summary>
	public void CopyBlock(string blockName) {
		foreach (var par in CopyBlockList(blockName)) {
			outBody.Append(par.CloneNode(true));
		}
	}

	/// <summary>
	/// 複製範本Block(指定來源)
	/// </summary>
	/// <param name="srcDocName">來源範本別名</param>
	public void CopyBlock(string srcDocName, string blockName) {
		foreach (var par in CopyBlockList(srcDocName, blockName)) {
			outBody.Append(par.CloneNode(true));
		}
	}
	#endregion

	#region 複製範本Block,回傳Dictionary
	/// <summary>
	/// 複製範本Block,回傳Dictionary
	/// </summary>
	public Dictionary<int, OpenXmlElement> CopyBlockDict(string blockName) {
		return CopyBlockDict(defTplDocName, blockName);
	}

	/// <summary>
	/// 複製範本Block,回傳Dictionary
	/// </summary>
	/// <param name="srcDocName">來源範本別名</param>
	public Dictionary<int, OpenXmlElement> CopyBlockDict(string srcDocName, string blockName) {
		try {
			WordprocessingDocument srcDoc = tplDoc[srcDocName];
			Dictionary<int, OpenXmlElement> dictElement = new Dictionary<int, OpenXmlElement>();

			foreach (var x in CopyBlockList(srcDocName, blockName).Select((Entry, Index) => new { Entry, Index })) {
				dictElement.Add(x.Index + 1, x.Entry);
			}
			return dictElement;
		}
		catch (Exception ex) {
			throw new Exception("複製範本Block!!(" + blockName + ")", ex);
		}
	}
	#endregion
	
	#region 複製範本Block,並取代文字
	/// <summary>
	/// 複製範本Block,並取代文字
	/// </summary>
	public void CopyReplaceBlock(string blockName, string searchStr, string newStr) {
		CopyReplaceBlock(blockName, new Dictionary<string, string>() { { searchStr, newStr } });
	}
	/// <summary>
	/// 複製範本Block,並取代文字(指定來源)
	/// </summary>
	/// <param name="srcDocName">來源範本別名</param>
	public void CopyReplaceBlock(string srcDocName, string blockName, string searchStr, string newStr) {
		CopyReplaceBlock(srcDocName, blockName, new Dictionary<string, string>() { { searchStr, newStr } });
	}
	/// <summary>
	/// 複製範本Block,並取代文字
	/// </summary>
	public void CopyReplaceBlock(string blockName, Dictionary<string, string> mappingDic) {
		CopyReplaceBlock(defTplDocName, blockName, mappingDic);
	}
	/// <summary>
	/// 複製範本Block,並取代文字(指定來源)
	/// </summary>
	/// <param name="srcDocName">來源範本別名</param>
	public void CopyReplaceBlock(string srcDocName, string blockName, Dictionary<string, string> mappingDic) {
		int i = 0;
		try {
			List<OpenXmlElement> pars = CopyBlockList(srcDocName, blockName);
			for (i=0; i < pars.Count; i++) {
				string tmpInnerText = pars[i].InnerText;
				foreach (var item in mappingDic) {
					tmpInnerText = tmpInnerText.Replace(item.Key, item.Value);
				}
				Run parRun = pars[i].Descendants<Run>().FirstOrDefault();
				pars[i].RemoveAllChildren<Run>();
				if (parRun != null) {
					parRun.RemoveAllChildren<Text>();
					parRun.Append(new Text(tmpInnerText));
					pars[i].Append(parRun.CloneNode(true));
				}
			}
			outBody.Append(pars.ToArray());
		}
		catch (Exception ex) {
			throw new Exception("複製範本Block錯誤!!(" + blockName + "," + i + ")", ex);
		}
	}
	#endregion

	#region 取代書籤
	/// <summary>
	/// 取代書籤
	/// </summary>
	/// <param name="bookmarkName">書籤名稱</param>
	public void ReplaceBookmark(string bookmarkName, string text) {
		try {
			MainDocumentPart mainPart = outDoc.MainDocumentPart;
			IEnumerable<BookmarkEnd> bookMarkEnds = mainPart.RootElement.Descendants<BookmarkEnd>();
			foreach (BookmarkStart bookmarkStart in mainPart.RootElement.Descendants<BookmarkStart>()) {
				if (bookmarkStart.Name.Value.ToLower() == bookmarkName.ToLower()) {
					string id = bookmarkStart.Id.Value;
					//BookmarkEnd bookmarkEnd = bookMarkEnds.Where(i => i.Id.Value == id).First();
					BookmarkEnd bookmarkEnd = bookMarkEnds.Where(i => i.Id.Value == id).FirstOrDefault();

					////var bookmarkText = bookmarkEnd.NextSibling();
					//Run bookmarkRun = bookmarkStart.NextSibling<Run>();
					//if (bookmarkRun != null) {
					//	string[] txtArr = text.Split('\n');
					//	for (int i = 0; i < txtArr.Length; i++) {
					//		if (i == 0) {
					//			bookmarkRun.GetFirstChild<Text>().Text = txtArr[i];
					//		} else {
					//			bookmarkRun.Append(new Break());
					//			bookmarkRun.Append(new Text(txtArr[i]));
					//		}
					//	}
					//}
					Run bookmarkRun = bookmarkStart.NextSibling<Run>();
					if (bookmarkRun != null) {
						Run tplRun = bookmarkRun;
						string[] txtArr = text.Split('\n');
						for (int i = 0; i < txtArr.Length; i++) {
							if (i == 0) {
								bookmarkRun.GetFirstChild<Text>().Text = txtArr[i];
							} else {
								bookmarkRun.Append(new Break());
								bookmarkRun.Append(new Text(txtArr[i]));
							}
						}
						int j = 0;
						while (tplRun.NextSibling() != null && tplRun.NextSibling().GetType() != typeof(BookmarkEnd)) {
							j++;
							tplRun.NextSibling().Remove();
							if (j >= 20)
								break;
						}
					}
					bookmarkStart.Remove();
					if (bookmarkEnd != null) bookmarkEnd.Remove();
				}
			}
		}
		catch (Exception ex) {
			throw new Exception("取代書籤錯誤!!(" + bookmarkName + ")", ex);
		}
	}
	#endregion

	#region 複製範本頁尾
	/// <summary>
	/// 複製範本頁尾
	/// </summary>
	/// <param name="srcDocName">來源範本別名</param>
	/// <param name="isNewChapter">是否帶分節符號(新章節)</param>
	public void CopyPageFoot(string srcDocName, bool isNewChapter) {
		WordprocessingDocument sourceDoc = tplDoc[srcDocName];
		int index = 0;//取消index參數,只抓第1個

		string newRefId = string.Format("foot_{0}", Guid.NewGuid().ToString().Substring(0, 8));

		FooterReference[] footer = sourceDoc.MainDocumentPart.RootElement.Descendants<FooterReference>().ToArray();
		string srcRefId = footer[index].Id;
		footer[index].Id = newRefId;

		FooterPart elementFoot = sourceDoc.MainDocumentPart.FooterParts
		.Where(
			element => sourceDoc.MainDocumentPart.GetIdOfPart(element) == srcRefId
		).SingleOrDefault();

		outDoc.MainDocumentPart.AddPart(elementFoot, newRefId);

		if (isNewChapter) {
			FooterPart fp = outDoc.MainDocumentPart.FooterParts
			.Where(
				element => outDoc.MainDocumentPart.GetIdOfPart(element) == newRefId
			).SingleOrDefault();
			ParagraphStyleId pStyle = fp.Footer.Descendants<ParagraphStyleId>().First();
			string oldStyleId = pStyle.Val;
			string newStyleId = string.Format("fs_{0}", Guid.NewGuid().ToString().Substring(0, 8));
			pStyle.Val = newStyleId;

			StylesPart srcPart = sourceDoc.MainDocumentPart.StyleDefinitionsPart;
			Style st = srcPart.Styles.Descendants<Style>()
			.Where(
				element => element.StyleId == oldStyleId
			).SingleOrDefault();
			st.StyleId = newStyleId;

			StylesPart outPart = outDoc.MainDocumentPart.StyleDefinitionsPart;
			outPart.Styles.Append(st.CloneNode(true));

			//outBody.AppendChild(new Paragraph(new ParagraphProperties(footer[index].Parent.CloneNode(true))));//頁尾+分節符號
			//throw new Exception(footer[index].Parent.InnerXml);
			outBody.Append(new Paragraph(new ParagraphProperties(footer[index].Parent.CloneNode(true))));//頁尾+分節符號
			//OpenXmlElement[] elements = footerSections[index].Parent.Descendants().ToArray();
			//Paragraph par = new Paragraph();
			//ParagraphProperties section = new ParagraphProperties();
			//foreach (var item in elements) {
			//	section.AppendChild(item.CloneNode(true));
			//}
			//par.AppendChild(section.CloneNode(true));
			//outBody.AppendChild(par.CloneNode(true));//頁尾+分節符號
		} else {
			outBody.AppendChild(footer[index].Parent.CloneNode(true));//頁尾
		}
	}
	#endregion

	#region 插入圖片
	/// <summary>
	/// 插入圖片
	/// </summary>
	//public void AppendImage(string imgStr, bool isBase64, decimal scale) {
	//	ImageData img= new ImageData(imgStr, isBase64, scale);
	public void AppendImage(ImageFile img) {
		ImagePart imagePart = outDoc.MainDocumentPart.AddImagePart(ImagePartType.Jpeg);
		string relationshipId = outDoc.MainDocumentPart.GetIdOfPart(imagePart);
		imagePart.FeedData(img.getDataStream());

		// Define the reference of the image.
		var element =
			 new Drawing(
				 new DW.Inline(
					//Size of image, unit = EMU(English Metric Unit)
					//1 cm = 360000 EMUs
					 new DW.Extent() { Cx = img.GetWidthInEMU(), Cy = img.GetHeightInEMU() },
					 new DW.EffectExtent()
					 {
						 LeftEdge = 0L,
						 TopEdge = 0L,
						 RightEdge = 0L,
						 BottomEdge = 0L
					 },
					 new DW.DocProperties()
					 {
						 Id = (UInt32Value)1U,
						 Name = img.ImageName
					 },
					 new DW.NonVisualGraphicFrameDrawingProperties(
						 new A.GraphicFrameLocks() { NoChangeAspect = true }),
					 new A.Graphic(
						 new A.GraphicData(
							 new PIC.Picture(
								 new PIC.NonVisualPictureProperties(
									 new PIC.NonVisualDrawingProperties()
									 {
										 Id = (UInt32Value)0U,
										 Name = img.FileName
									 },
									 new PIC.NonVisualPictureDrawingProperties()),
								 new PIC.BlipFill(
									 new A.Blip(
										 new A.BlipExtensionList(
											 new A.BlipExtension()
											 {
												 Uri =
													"{28A0092B-C50C-407E-A947-70E740481C1C}"
											 })
									 )
									 {
										 Embed = relationshipId,
										 CompressionState =
										 A.BlipCompressionValues.Print
									 },
									 new A.Stretch(
										 new A.FillRectangle())),
								 new PIC.ShapeProperties(
									 new A.Transform2D(
										 new A.Offset() { X = 0L, Y = 0L },
										 new A.Extents()
										 {
											 Cx = img.GetWidthInEMU(),
											 Cy = img.GetHeightInEMU()
										 }),
									 new A.PresetGeometry(
										 new A.AdjustValueList()
									 ) { Preset = A.ShapeTypeValues.Rectangle }))
						 ) { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" })
				 )
				 {
					 DistanceFromTop = (UInt32Value)0U,
					 DistanceFromBottom = (UInt32Value)0U,
					 DistanceFromLeft = (UInt32Value)0U,
					 DistanceFromRight = (UInt32Value)0U,
					 //EditId = "50D07946"
				 });

		outDoc.MainDocumentPart.Document.Body.AppendChild(new Paragraph(new Run(element)));
	}
	#endregion
	
	#region 增加段落
	/// <summary>
	/// 增加段落
	/// </summary>
	public void AddParagraph(Paragraph par) {
		//outDoc.MainDocumentPart.Document.Body.Append(par.CloneNode(true));
		outBody.Append(par.CloneNode(true));
	}
	#endregion

	#region 增加段落
	/// <summary>
	/// 增加段落
	/// </summary>
	public OpenXmlHelper AddParagraph() {
		outBody.Append(new Paragraph(new Run()));
		return this;
	}
	#endregion

	#region 增加文字
	/// <summary>
	/// 在文件最後的段落加上文字
	/// </summary>
	public OpenXmlHelper AddText(string text) {
		Run LastRun = outDoc.MainDocumentPart.RootElement.Descendants<Run>().LastOrDefault();
		if (LastRun == null) {
			outBody.AppendChild(new Paragraph(new Run()));
			LastRun = outDoc.MainDocumentPart.RootElement.Descendants<Run>().LastOrDefault();
		}

		string[] txtArr = text.Split('\n');
		for (int i = 0; i < txtArr.Length; i++) {
			if (i != 0) {
				LastRun.Append(new Break());
			}
			LastRun.Append(new Text(txtArr[i]));
		}
		return this;
	}
	#endregion

	#region 插入換行符號(Shift-Enter)
	/// <summary>
	/// 插入換行符號(Shift-Enter)
	/// </summary>
	public OpenXmlHelper NewLine() {
		Run LastRun = outDoc.MainDocumentPart.RootElement.Descendants<Run>().LastOrDefault();
		if (LastRun != null) {
			LastRun.Append(new Break());
		}
		return this;
	}
	#endregion

	#region 插入分頁符號(Ctrl-Enter)
	/// <summary>
	/// 插入分頁符號(Ctrl-Enter)
	/// </summary>
	public OpenXmlHelper NewPage() {
		outBody.AppendChild(new Paragraph(new Run(new Break() { Type = BreakValues.Page })));//分頁符號
		return this;
	}
	#endregion

	#region 設定紙張大小
	/// <summary>
	/// 設定紙張大小
	/// </summary>
	/// <param name="widthCM">寬(公分)</param>
	/// <param name="heightCM">高(公分)</param>
	public OpenXmlHelper SetPageSize(double widthCM, double heightCM) {
		//SectionProperties sections0 = outDoc.MainDocumentPart.Document.Body.Elements<SectionProperties>().FirstOrDefault();
		//SectionProperties sections0 = outDoc.MainDocumentPart.RootElement.Descendants<SectionProperties>().FirstOrDefault();
		//SectionProperties sectPr = outDoc.MainDocumentPart.Document.Descendants<SectionProperties>().FirstOrDefault();
		if (outDoc.MainDocumentPart.Document.Descendants<SectionProperties>().FirstOrDefault() == null) {
			outBody.Append(new SectionProperties());
		}

		var sections = outDoc.MainDocumentPart.Document.Descendants<SectionProperties>();
		foreach (SectionProperties sectPr in sections) {
			//PageSize pageSize = sections0.GetFirstChild<PageSize>();
			PageSize pgSz = sectPr.Descendants<PageSize>().FirstOrDefault();
			if (pgSz == null) {
				//pageSize = new PageSize() { Width = (UInt32Value)11906U, Height = (UInt32Value)16838U };
				//pageSize = new PageSize() { Width = 11906, Height = 16838, Orient = PageOrientationValues.Portrait };//21、29.7//直向
				//pageSize = new PageSize() { Width = (UInt32Value)16838U, Height = (UInt32Value)11906U, Orient = PageOrientationValues.Landscape };
				//pageSize = new PageSize() { Width = 16838, Height = 11906, Orient = PageOrientationValues.Landscape };//橫向
				pgSz = new PageSize();
				sectPr.Append(pgSz);
			}
			pgSz.Height = Convert.ToUInt32(Math.Round((decimal)heightCM * (decimal)566.9523, 0));
			pgSz.Width = Convert.ToUInt32(Math.Round((decimal)widthCM * (decimal)566.9523, 0));
		}

		return this;
	}
	#endregion

	#region 設為直向
	/// <summary>
	/// 設為直向
	/// </summary>
	public OpenXmlHelper SetPagePortrait() {
		return SetPageOrientation(PageOrientationValues.Portrait);
	}
	#endregion

	#region 設為橫向
	/// <summary>
	/// 設為橫向
	/// </summary>
	public OpenXmlHelper SetPageLandscape() {
		return SetPageOrientation(PageOrientationValues.Landscape);
	}
	#endregion

	#region 設定方向
	/// <summary>
	/// 設定方向,需先設定紙張大小,否則無作用
	/// </summary>
	/// <param name="newOrientation">Landscape:橫向;Portrait:直向</param>
	protected OpenXmlHelper SetPageOrientation(PageOrientationValues newOrientation) {
		var sections = outDoc.MainDocumentPart.Document.Descendants<SectionProperties>();
		foreach (SectionProperties sectPr in sections) {
			bool pageOrientationChanged = false;

			PageSize pgSz = sectPr.Descendants<PageSize>().FirstOrDefault();
			if (pgSz != null) {
				if (pgSz.Orient == null) {
					if (newOrientation != PageOrientationValues.Portrait) {
						pgSz.Orient = new EnumValue<PageOrientationValues>(newOrientation);
						pageOrientationChanged = true;
					}
				} else {
					if (pgSz.Orient.Value != newOrientation) {
						pgSz.Orient.Value = newOrientation;
						pageOrientationChanged = true;
					}
				}
				if (pageOrientationChanged) {
					var width = pgSz.Width;
					var height = pgSz.Height;
					pgSz.Width = height;
					pgSz.Height = width;

					PageMargin pgMar = sectPr.Descendants<PageMargin>().FirstOrDefault();
					if (pgMar != null) {
						var top = pgMar.Top.Value;
						var bottom = pgMar.Bottom.Value;
						var left = pgMar.Left.Value;
						var right = pgMar.Right.Value;

						pgMar.Top = new Int32Value((int)left);
						pgMar.Bottom = new Int32Value((int)right);
						pgMar.Left = new UInt32Value((uint)Math.Max(0, bottom));
						pgMar.Right = new UInt32Value((uint)Math.Max(0, top));
					}
				}
			}
		}
		return this;
	}
	#endregion
}

#region ImageFile
public class ImageFile {
	public string FileName = string.Empty;

	public byte[] BinaryData;

	public Stream getDataStream() {
		//Stream DataStream = new MemoryStream(BinaryData);
		return new MemoryStream(BinaryData);
	}

	public ImagePartType ImageType {
		get {
			var ext = Path.GetExtension(FileName).TrimStart('.').ToLower();
			switch (ext) {
				case "jpg":
					return ImagePartType.Jpeg;
				case "png":
					return ImagePartType.Png;
				case "bmp":
					return ImagePartType.Bmp;
			}
			throw new ApplicationException(string.Format("不支援的格式:{0}", ext));
		}
	}

	public int SourceWidth;
	public int SourceHeight;
	public decimal Width;
	public decimal Height;

	//public long WidthInEMU => Convert.ToInt64(Width * CM_TO_EMU);
	private long WidthInEMU = 0;
	public long GetWidthInEMU() {
		WidthInEMU = Convert.ToInt64(Width * CM_TO_EMU);
		return WidthInEMU;
	}

	//public long HeightInEMU => Convert.ToInt64(Height * CM_TO_EMU);
	private long HeightInEMU = 0;
	public long GetHeightInEMU() {
		HeightInEMU = Convert.ToInt64(Height * CM_TO_EMU);
		return HeightInEMU;
	}

	private const decimal INCH_TO_CM = 2.54M;
	private const decimal CM_TO_EMU = 360000M;
	public string ImageName;

	public ImageFile(string fileName, byte[] data, decimal scale) {
		if (fileName == "") {
			FileName = string.Format("IMG_{0}", Guid.NewGuid().ToString().Substring(0, 8));
			ImageName = FileName;
		} else {
			FileName = fileName;
			ImageName = string.Format("IMG_{0}", Guid.NewGuid().ToString().Substring(0, 8));
		}

		BinaryData = data;
		int dpi = 300;
		Bitmap img = new Bitmap(new MemoryStream(data));
		SourceWidth = img.Width;
		SourceHeight = img.Height;
		Width = ((decimal)SourceWidth) / dpi * scale * INCH_TO_CM;
		Height = ((decimal)SourceHeight) / dpi * scale * INCH_TO_CM;
	}

	public ImageFile(byte[] data) :
		this("", data, 1) {
	}

	public ImageFile(byte[] data, decimal scale) :
		this("", data, scale) {
	}

	public ImageFile(string fileName) :
		this(fileName, File.ReadAllBytes(fileName), 1) {
	}

	public ImageFile(string fileName, decimal scale) :
		this(fileName, File.ReadAllBytes(fileName), scale) {
	}
}
#endregion