using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Text;
using HtmlAgilityPack;

namespace BouvetContacts
{
    class OrganizationRetriever : WebRetriever
    {
        private const string OrgURL = "/Modules/info/treeview.asp?id={384EFC19-0A9F-47A3-8D75-1086FB2E5F79}&structureid=&tab=2";

        public OrganizationRetriever(NetworkCredential credentials) : base(credentials)
        {
        }

        public Organization GetOrganization()
        {
            var html = GetHTML(OrgURL);
            var document = new HtmlAgilityPack.HtmlDocument();
            document.LoadHtml(html);

            var rootTable = document.DocumentNode.SelectSingleNode("//table");

            return getOrgFromTable(rootTable);
        }

        private Organization getOrgFromTable(HtmlNode table)
        {
            var ret = new Organization();
            ret.Name = table.InnerText;
            ret.PeopleURL = table.SelectSingleNode(".//a").GetAttributeValue("href", null);

            var subOrgs = table.SelectSingleNode("following-sibling::*").SelectNodes("./tr/td/div[@class='ob_d2b']");
            if (subOrgs != null)
            {
                ret.Children = new List<Organization>();
                foreach (var subOrg in subOrgs)
                {
                    ret.Children.Add(getOrgFromTable(subOrg.SelectSingleNode("./table")));
                }
            }
            return ret;
        }
    }
}
