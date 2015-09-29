﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Wyam.Common;

namespace Wyam.Modules.CodeAnalysis
{
    internal class XmlDocumentationParser
    {
        private readonly ISymbol _symbol;
        private readonly ConcurrentDictionary<string, IDocument> _commentIdToDocument;
        private readonly ConcurrentDictionary<string, string> _cssClasses;
        private readonly ITrace _trace;
        private bool _parsed;
        private IReadOnlyList<KeyValuePair<string, IReadOnlyList<string>>> _exampleHtml 
            = ImmutableArray<KeyValuePair<string, IReadOnlyList<string>>>.Empty;
        private IReadOnlyList<KeyValuePair<string, IReadOnlyList<string>>> _remarksHtml
            = ImmutableArray<KeyValuePair<string, IReadOnlyList<string>>>.Empty;
        private IReadOnlyList<KeyValuePair<string, IReadOnlyList<string>>> _summaryHtml
            = ImmutableArray<KeyValuePair<string, IReadOnlyList<string>>>.Empty;
        private IReadOnlyList<KeyValuePair<string, string>> _exceptionHtml 
            = ImmutableArray<KeyValuePair<string, string>>.Empty;
        private IReadOnlyList<KeyValuePair<string, string>> _paramHtml
            = ImmutableArray<KeyValuePair<string, string>>.Empty;
        private IReadOnlyList<KeyValuePair<string, string>> _permissionHtml
            = ImmutableArray<KeyValuePair<string, string>>.Empty;
        private IReadOnlyList<KeyValuePair<string, IReadOnlyList<string>>> _returnsHtml 
            = ImmutableArray<KeyValuePair<string, IReadOnlyList<string>>>.Empty;
        private IReadOnlyList<string> _seeAlsoHtml = ImmutableArray<string>.Empty; 

        public XmlDocumentationParser(ISymbol symbol, ConcurrentDictionary<string, IDocument> commentIdToDocument,
            ConcurrentDictionary<string, string> cssClasses, ITrace trace)
        {
            _symbol = symbol;
            _commentIdToDocument = commentIdToDocument;
            _trace = trace;
            _cssClasses = cssClasses;
        }

        public IReadOnlyList<KeyValuePair<string, IReadOnlyList<string>>> GetExampleHtml()
        {
            Parse();
            return _exampleHtml;
        }

        public IReadOnlyList<KeyValuePair<string, IReadOnlyList<string>>> GetRemarksHtml()
        {
            Parse();
            return _remarksHtml;
        }

        public IReadOnlyList<KeyValuePair<string, IReadOnlyList<string>>> GetSummaryHtml()
        {
            Parse();
            return _summaryHtml;
        }

        public IReadOnlyList<KeyValuePair<string, string>> GetExceptionHtml()
        {
            Parse();
            return _exceptionHtml;
        }

        public IReadOnlyList<KeyValuePair<string, string>> GetParamHtml()
        {
            Parse();
            return _paramHtml;
        }

        public IReadOnlyList<KeyValuePair<string, string>> GetPermissionHtml()
        {
            Parse();
            return _permissionHtml;
        }

        public IReadOnlyList<KeyValuePair<string, IReadOnlyList<string>>> GetReturnsHtml()
        {
            Parse();
            return _returnsHtml;
        }

        public IReadOnlyList<string> GetSeeAlsoHtml()
        {
            Parse();
            return _seeAlsoHtml;
        }

        private void Parse()
        {
            if (_parsed)
            {
                return;
            }

            string documentationCommentXml;
            if (_symbol != null && !string.IsNullOrWhiteSpace(
                documentationCommentXml = _symbol.GetDocumentationCommentXml(expandIncludes: true)))
            {
                try
                {
                    // We shouldn't need a root element, the compiler adds a "<member name='Foo.Bar'>" root for us
                    XDocument xdoc = XDocument.Parse(documentationCommentXml, LoadOptions.PreserveWhitespace);
                    _exampleHtml = ProcessRootElement(xdoc.Root, "example");
                    _remarksHtml = ProcessRootElement(xdoc.Root, "remarks");
                    _summaryHtml = ProcessRootElement(xdoc.Root, "summary");
                    _exceptionHtml = ProcessExceptionElements(xdoc.Root);
                    _paramHtml = ProcessParamElements(xdoc.Root);
                    _permissionHtml = ProcessPermissionElements(xdoc.Root);
                    _returnsHtml = ProcessRootElement(xdoc.Root, "returns");
                    _seeAlsoHtml = ProcessChildSeeAlsoElements(xdoc.Root);
                }
                catch (Exception ex)
                {
                    _trace.Warning($"Could not parse XML documentation comments for {_symbol.Name}: {ex.Message}");
                }
            }

            _parsed = true;
        }

        // <example>, <remarks>, <summary>, <returns>
        private IReadOnlyList<KeyValuePair<string, IReadOnlyList<string>>> ProcessRootElement(XElement root, string elementName)
        {
            return root.Elements(elementName).Select(element =>
            {
                IReadOnlyList<string> seealso = ProcessChildSeeAlsoElements(element);
                ProcessChildElements(element);
                AddCssClasses(element);

                // Return InnerXml
                XmlReader reader = element.CreateReader();
                reader.MoveToContent();
                return new KeyValuePair<string, IReadOnlyList<string>>(reader.ReadInnerXml(), seealso);
            }).ToImmutableArray();
        }

        // <exception>
        private IReadOnlyList<KeyValuePair<string, string>> ProcessExceptionElements(XElement root)
        {
            return root.Elements("exception").Select(exceptionElement =>
            {
                bool link;
                string linkOrName = GetCrefLinkOrName(exceptionElement, out link);
                ProcessChildElements(exceptionElement);
                AddCssClasses(exceptionElement);
                XmlReader reader = exceptionElement.CreateReader();
                reader.MoveToContent();
                return new KeyValuePair<string, string>(linkOrName, reader.ReadInnerXml());
            }).ToImmutableArray();
        }

        // <param>
        private IReadOnlyList<KeyValuePair<string, string>> ProcessParamElements(XElement root)
        {
            return root.Elements("param").Select(paramElement =>
            {
                XAttribute nameAttribute = paramElement.Attribute("name");
                string name = nameAttribute?.Value ?? string.Empty;
                ProcessChildElements(paramElement);
                AddCssClasses(paramElement);
                XmlReader reader = paramElement.CreateReader();
                reader.MoveToContent();
                return new KeyValuePair<string, string>(name, reader.ReadInnerXml());
            }).ToImmutableArray();
        }

        // <permission>
        private IReadOnlyList<KeyValuePair<string, string>> ProcessPermissionElements(XElement root)
        {
            return root.Elements("permission").Select(permissionElement =>
            {
                bool link;
                string linkOrName = GetCrefLinkOrName(permissionElement, out link);
                ProcessChildElements(permissionElement);
                AddCssClasses(permissionElement);
                XmlReader reader = permissionElement.CreateReader();
                reader.MoveToContent();
                return new KeyValuePair<string, string>(linkOrName, reader.ReadInnerXml());
            }).ToImmutableArray();
        }

        private string GetCrefLinkOrName(XElement element, out bool link)
        {
            XAttribute crefAttribute = element.Attribute("cref");
            IDocument crefDoc;
            if (crefAttribute != null && _commentIdToDocument.TryGetValue(crefAttribute.Value, out crefDoc))
            {
                link = true;
                return $"<a href=\"{crefDoc.Link(MetadataKeys.WritePath)}\">{crefDoc[MetadataKeys.DisplayName]}</a>";
            }
            link = false;
            return crefAttribute?.Value.Substring(crefAttribute.Value.IndexOf(':') + 1) ?? string.Empty;
        }

        // Adds/updates CSS classes for all nested elements
        private void AddCssClasses(XElement parentElement)
        {
            foreach (XElement element in parentElement.Descendants())
            {
                string cssClasses;
                if (_cssClasses.TryGetValue(element.Name.ToString(), out cssClasses) && !string.IsNullOrWhiteSpace(cssClasses))
                {
                    AddCssClasses(element, cssClasses);
                }
            }
        }

        private void AddCssClasses(XElement element, string cssClasses)
        {
            XAttribute classAttribute = element.Attribute("class");
            if (classAttribute != null)
            {
                classAttribute.Value = classAttribute.Value + " " + cssClasses;
            }
            else
            {
                element.Add(new XAttribute("class", cssClasses));
            }
        }

        // Groups all the nested element processing together so it can be used from multiple parent elements
        private void ProcessChildElements(XElement parentElement)
        {
            ProcessChildCodeElements(parentElement);
            ProcessChildCElements(parentElement);
            ProcessChildListElements(parentElement);
            ProcessChildParaElements(parentElement);
            ProcessChildParamrefElements(parentElement);
            ProcessChildSeeElements(parentElement);
        }

        // <code>
        private void ProcessChildCodeElements(XElement parentElement)
        {
            foreach (XElement codeElement in parentElement.Elements("code"))
            {
                codeElement.ReplaceWith(new XElement("pre", codeElement));
            }
        }

        // <c>
        private void ProcessChildCElements(XElement parentElement)
        {
            foreach (XElement cElement in parentElement.Elements("c"))
            {
                cElement.Name = "code";
            }
        }

        // <list>
        private void ProcessChildListElements(XElement parentElement)
        {
            foreach (XElement listElement in parentElement.Elements("list"))
            {
                XAttribute typeAttribute = listElement.Attribute("type");
                if (typeAttribute != null && typeAttribute.Value == "table")
                {
                    ProcessListElementTable(listElement, typeAttribute);
                }
                else
                {
                    ProcessListElementList(listElement, typeAttribute);
                }
            }
        }

        private void ProcessListElementList(XElement listElement, XAttribute typeAttribute)
        {
            // Number or bullet
            if (typeAttribute != null && typeAttribute.Value == "number")
            {
                listElement.Name = "ol";
            }
            else
            {
                listElement.Name = "ul";
            }
            typeAttribute?.Remove();

            // Replace children
            foreach(XElement itemElement in listElement.Elements("listheader")
                .Concat(listElement.Elements("item")).ToList())
            {
                foreach (XElement termElement in itemElement.Elements("term"))
                {
                    termElement.Name = "span";
                    AddCssClasses(termElement, "term");
                    ProcessChildElements(termElement);
                }
                foreach (XElement descriptionElement in itemElement.Elements("description"))
                {
                    descriptionElement.Name = "span";
                    AddCssClasses(descriptionElement, "description");
                    ProcessChildElements(descriptionElement);
                }

                itemElement.Name = "li";
            }
        }

        private void ProcessListElementTable(XElement listElement, XAttribute typeAttribute)
        {
            listElement.Name = "table";
            typeAttribute?.Remove();
            
            foreach (XElement itemElement in listElement.Elements("listheader")
                .Concat(listElement.Elements("item")).ToList())
            {
                foreach (XElement termElement in itemElement.Elements("term"))
                {
                    termElement.Name = itemElement.Name == "listheader" ? "th" : "td";
                    ProcessChildElements(termElement);
                }

                itemElement.Name = "tr";
            }
        }

        // <para>
        private void ProcessChildParaElements(XElement parentElement)
        {
            foreach (XElement paraElement in parentElement.Elements("para"))
            {
                paraElement.Name = "p";
                ProcessChildElements(paraElement);
            }
        }

        // <paramref>
        private void ProcessChildParamrefElements(XElement parentElement)
        {
            foreach (XElement paramrefElement in parentElement.Elements("paramref"))
            {
                XAttribute nameAttribute = paramrefElement.Attribute("name");
                paramrefElement.Value = nameAttribute?.Value ?? string.Empty;
                paramrefElement.Name = "span";
                AddCssClasses(paramrefElement, "paramref");
            }
        }

        // <see>
        private void ProcessChildSeeElements(XElement parentElement)
        {
            foreach (XElement seeElement in parentElement.Elements("see"))
            {
                bool link;
                string linkOrName = GetCrefLinkOrName(seeElement, out link);
                seeElement.ReplaceWith(link ? (object)XElement.Parse(linkOrName) : linkOrName);
            }
        }

        // <seealso>
        private IReadOnlyList<string> ProcessChildSeeAlsoElements(XElement parentElement)
        {
            List<string> seealso = new List<string>();
            foreach (XElement seealsoElement in parentElement.Elements("seealso").ToList())
            {
                bool link;
                seealso.Add(GetCrefLinkOrName(seealsoElement, out link));
                seealsoElement.Remove();
            }
            return seealso.ToImmutableArray();
        } 
    }
}
