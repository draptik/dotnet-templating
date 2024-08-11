namespace TemplatingLib

open System.IO
open System.Xml.Linq
open Errors
open TemplatingLib.Types

module Xml =

    let removeFirstElementFromXml (xml: string) (element: string) =
        let doc = XDocument.Parse xml
        let head = doc.Descendants(element) |> Seq.head
        head.Remove()
        doc.ToString()

    let removeElementWithChildFromXml (xml: string) (element: string) (childName: string) =
        let doc = XDocument.Parse xml

        let matchingElement =
            doc.Descendants(element)
            |> Seq.tryFind (fun ig -> ig.Elements() |> Seq.exists (fun e -> e.Name.LocalName = childName))

        match matchingElement with
        | Some elementToRemove -> elementToRemove.Remove()
        | None -> ()

        doc.ToString()

    let removeFirstPropertyGroupFromXml (xml: string) =
        removeFirstElementFromXml xml "PropertyGroup"

    let removeItemGroupWithPackagesFromXml (xml: string) =
        removeElementWithChildFromXml xml "ItemGroup" "PackageReference"

    let getConfigFile (language: Language) (path: ValidatedPath) =
        let ext = languageToConfigExtension language
        $"{path}.{ext}"

    let tryModifyingDotnetXmlConfig language unmodifiedConfigFile fn errType =
        let file = getConfigFile language unmodifiedConfigFile

        try
            let xmlString = File.ReadAllText file
            let newXml = fn xmlString
            File.WriteAllText(file, newXml) |> Ok
        with e ->
            Error(errType e.Message)

    let tryRemovePropertyGroupFromFile (language: Language) (unmodifiedConfigFile: ValidatedPath) =
        tryModifyingDotnetXmlConfig
            language
            unmodifiedConfigFile
            removeFirstPropertyGroupFromXml
            CantRemovePropertyGroup

    let tryRemoveItemGroupFromFile (language: Language) (unmodifiedConfigFile: ValidatedPath) =
        tryModifyingDotnetXmlConfig language unmodifiedConfigFile removeItemGroupWithPackagesFromXml CantRemoveItemGroup


    let addElement (xml: string) (elementToAdd: XElement) =
        let doc = XDocument.Parse xml
        doc.Root.Add elementToAdd
        doc.ToString()

    let addGeneratedDocumentationFile xml =
        let elementToAdd = XElement("PropertyGroup", XElement("GenerateDocumentationFile", "true"))
        addElement xml elementToAdd
        
    let tryAddPropertyGroupGenerateDocumentationFile (language: Language) (unmodifiedConfigFile: ValidatedPath) =
        tryModifyingDotnetXmlConfig
            language
            unmodifiedConfigFile
            addGeneratedDocumentationFile
            CantAddGeneratePropertyGroupWithDocumentationFile
