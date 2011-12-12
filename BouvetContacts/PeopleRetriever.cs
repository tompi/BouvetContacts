using System;
using System.Collections.Generic;
using System.Net;
using HtmlAgilityPack;
using Thought.vCards;

namespace BouvetContacts
{
    class PeopleRetriever : WebRetriever
    {
        public PeopleRetriever(NetworkCredential credentials) : base(credentials)
        {
        }

        public List<vCard> GetVCards(string containerURL)
        {
            var ret = new List<vCard>();
            var html = GetHTML("/Modules/info/" + containerURL);
            var document = new HtmlDocument();
            document.LoadHtml(html);
            var pplTRS = document.DocumentNode.SelectNodes("//tr[@bgcolor='#CCCCCC']");

            foreach (var personTr in pplTRS)
            {
                var vcard = GetVCardFromTr(personTr, personTr.SelectSingleNode("following-sibling::*"));
                if (vcard!= null)
                {
                    ret.Add(vcard);
                }
            }
            return ret;
        }

        private vCard GetVCardFromTr(HtmlNode personTr, HtmlNode detailsTr)
        {
            var ret = new vCard();
            var personTDs = personTr.SelectNodes("./td");
            ret.FamilyName = personTDs[0].InnerText;
            ret.GivenName = personTDs[1].InnerText;
            ret.DisplayName = 
            ret.Office = personTDs[2].InnerText;
            var workPhone = getPhone(personTDs[3].InnerText);
            if (workPhone != null) ret.Phones.Add(new vCardPhone(workPhone, vCardPhoneTypes.WorkVoice));
            var mobile = getPhone(personTDs[4].InnerText);
            if (mobile != null) ret.Phones.Add(new vCardPhone(mobile, vCardPhoneTypes.CellularVoice));
            ret.Organization = "Bouvet";

            var detailsURL = personTDs[0].SelectSingleNode(".//a").GetAttributeValue("href", null);
            if (!string.IsNullOrEmpty(detailsURL))
            {
                var strippedURL =
                    detailsURL.Replace("javascript:fnPopUp('", "").Replace("', '60', '70')", "").Replace("amp;", "");
                var detailsHtml = GetHTML(strippedURL);
                var detailsDoc = new HtmlDocument();
                detailsDoc.LoadHtml(detailsHtml);

                // Displayname
                ret.DisplayName = detailsDoc.DocumentNode.SelectSingleNode("//title").InnerText;

                // Generelt


                // Fødselsdag
                var fodt = GetNextTDText(detailsDoc, "Fødselsdato");
                if (!string.IsNullOrEmpty(fodt))
                {
                    DateTime fDato;
                    if (DateTime.TryParse(fodt, out fDato))
                    {
                        ret.BirthDate = fDato;
                    }
                }
                // Title
                var title = GetNextTDText(detailsDoc, "Stilling");
                if (!string.IsNullOrEmpty(title))
                {
                    ret.Title = title;
                }
                // Email
                var email = detailsDoc.DocumentNode.SelectSingleNode("//a[@title='Send e-mail...&quot;>']");
                if (email != null)
                {
                    var workEmail = new vCardEmailAddress(email.InnerText, vCardEmailAddressType.Internet)
                                        {IsPreferred = true};
                    ret.EmailAddresses.Add(workEmail);
                }
                // MSN
                var msn = GetNextTDText(detailsDoc, "MSN");
                if (!string.IsNullOrEmpty(msn))
                {
                    ret.XMsn = msn;
                }

                // Kontor
                // Kontor-adresse
                var workAddr = GetAddress(detailsDoc, 0);
                if (workAddr != null)
                {
                    workAddr.IsHome = false;
                    ret.DeliveryAddresses.Add(workAddr);
                }


                // Privat
                // Tlf
                //var privTlf = getPhone(detailsDoc.DocumentNode.SelectSingleNode("//tr[td='Privat tlf']").SelectNodes("./td")[1].InnerText);
                var privTlf = getPhone(GetNextTDText(detailsDoc, "Privat tlf"));
                if (!string.IsNullOrEmpty(privTlf))
                {
                    ret.Phones.Add(new vCardPhone(privTlf, vCardPhoneTypes.HomeVoice));
                }
                // email
                var privEmail = GetNextTDText(detailsDoc, "Privat e-mail");
                if (!string.IsNullOrEmpty(privEmail))
                {
                    var privEmailAddr = new vCardEmailAddress(privEmail, vCardEmailAddressType.Internet)
                                            {IsPreferred = false};
                    ret.EmailAddresses.Add(privEmailAddr);
                }
                // Hjemme-adresse
                var homeAddr = GetAddress(detailsDoc, 1);
                if (homeAddr != null)
                {
                    homeAddr.IsHome = true;
                    ret.DeliveryAddresses.Add(homeAddr);
                }

                // Erfaring
                var erfaring = GetNextTDText(detailsDoc, "Bakgrunn");
                if (!string.IsNullOrEmpty(erfaring))
                {
                    ret.Notes.Add(new vCardNote(erfaring));
                }
                // Utdanning
                var utdanning = GetNextTDText(detailsDoc, "Utdannelse");
                if (!string.IsNullOrEmpty(utdanning))
                {
                    ret.Notes.Add(new vCardNote(utdanning));
                }
            }

            var img = detailsTr.SelectSingleNode(".//img");
            if (img != null)
            {
                var src = img.GetAttributeValue("src", null);
                if (src != null)
                {
                    using (var stream = GetStream(src))
                    {
                        ret.Photos.Add(new vCardPhoto(StreamHelper.ReadToEnd(stream)));
                    }
                }
            }
            return ret;
        }

        private vCardDeliveryAddress GetAddress(HtmlDocument detailsDoc, int ix)
        {
            var street = GetNextTDText(detailsDoc, "Gateadresse", ix);
            var box = GetNextTDText(detailsDoc, "Postboks", ix);
            var postalNr = GetNextTDText(detailsDoc, "Postnr", ix);
            var city = GetNextTDText(detailsDoc, "By", ix);
            var country = GetNextTDText(detailsDoc, "Land", ix);


            if (!string.IsNullOrEmpty(street + box + postalNr + city))
            {
                var address = new vCardDeliveryAddress();

                if (!string.IsNullOrEmpty(box))
                {
                    address.Street = box + "\n" + street;
                }
                else
                {
                    address.Street = street;
                }
                address.PostalCode = postalNr;
                address.City = city;
                address.Country = country;

                return address;
            }
            return null;
        }

        private string GetNextTDText(HtmlDocument detailsDoc, string firstTDContent, int ix = 0)
        {
            var tr = detailsDoc.DocumentNode.SelectNodes("//tr[td='" + firstTDContent + "']");
            if (tr != null && tr.Count>ix)
            {
                var tds = tr[ix].SelectNodes("./td");
                if (tds != null && tds.Count>1)
                {
                    var txt = tds[1].InnerText;
                    if (!string.IsNullOrEmpty(txt) && txt.Trim().Length>0)
                    {
                        return txt.Trim();
                    }
                    // Prøve på textarea
                    var ta = tds[1].SelectSingleNode("./textarea");
                    if (ta != null)
                    {
                        return ta.GetAttributeValue("Text", null);
                    }
                }
            }
            return null;
        }

        private string getPhone(string innerText)
        {
            if (string.IsNullOrEmpty(innerText)) return null;
            var ret = innerText.Replace("-", "").Trim();
            if (ret.Length > 0) return ret;
            return null;
        }

    }
}
