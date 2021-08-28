using System;
using System.Collections.Generic;
using System.Text;

namespace libAPX
{
    public class TransactionResult
    {
        public List<String> installedComponents { get; set; }

        public TransactionResult()
        {
            this.installedComponents = new List<string>();
        }
    }
}
