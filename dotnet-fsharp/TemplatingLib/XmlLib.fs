module TemplatingLib.XmlLib

let removeFromXml (xml: string) (element: string) =
    let doc = System.Xml.Linq.XDocument.Parse xml
    let head = doc.Descendants(element) |> Seq.head
    head.Remove()
    doc.ToString()

let removeFirstPropertyGroupFromXml (xml: string) = removeFromXml xml "PropertyGroup"

let removeFirstItemGroupFromXml (xml: string) = removeFromXml xml "ItemGroup"
