using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BouvetContacts
{
    class Organization
    {
        public String Name { get; set; }
        public String PeopleURL { get; set; }
        public List<Organization> Children { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
