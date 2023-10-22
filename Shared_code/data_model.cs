using System;
using System.Collections.Generic;

namespace org.iso.cinco.um
{
    public class Data_mdl
    {

        List<object> lista_dados = new List<object>();
        private string given_name;
        private DateTime birth_date;

        public Data_mdl(string given_name, DateTime birth_date) 
        {
            this.given_name = given_name;
            this.birth_date = birth_date;
            this.lista_dados.Add(new object[] { given_name, birth_date });
        }

        public override string ToString()
        {
            //List<String> list_string = new List<String>();
            //dados.given_name.ToString()
            return given_name + "," + this.birth_date.ToString();

        }
                        
    }
}
