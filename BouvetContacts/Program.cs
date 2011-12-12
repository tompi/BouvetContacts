using System;
using System.IO;
using System.Net;
using Thought.vCards;

namespace BouvetContacts
{
    class Program
    {
        private static NetworkCredential _credentials;
        static void Main()
        {
            Console.Write("Bruker: ");
            var user = Console.ReadLine();
            Console.Write("Passord: ");
            var password = Console.ReadLine();
            _credentials = new NetworkCredential(user, password, "bouvet");
            var orgRetriever = new OrganizationRetriever(_credentials);
            var root = orgRetriever.GetOrganization();


            WriteOrg(root, "");            

            //writeCards("show.asp?id={384EFC19-0A9F-47A3-8D75-1086FB2E5F79}&tab=0&searchlname=ALL&structureid={F427236D-0AF6-42F8-BB50-F9085A076BB4}", "test\\");
        }

        private static void WriteOrg(Organization org, string pathPrefix)
        {
            var folder = pathPrefix + org.Name + "\\";
            Directory.CreateDirectory(folder);
            Console.WriteLine(folder);

            if (org.Children == null)
            {
                WriteCards(org.PeopleURL, folder);
            } else
            {
                foreach (var child in org.Children)
                {
                    WriteOrg(child, folder);
                }
            }
        }

        private static void WriteCards(string peopleUrl, string folder)
        {
            var retriever = new PeopleRetriever(_credentials);
            var cards = retriever.GetVCards(peopleUrl);
            foreach (var card in cards)
            {
                var writer = new vCardStandardWriter
                                 {
                                     EmbedInternetImages = false,
                                     EmbedLocalImages = true,
                                     Options = vCardStandardWriterOptions.IgnoreCommas
                                 };

                var fileName = folder + card.DisplayName + ".vcf";
                writer.Write(card, fileName);
            }
        }
    }
}
