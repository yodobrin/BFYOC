using System;

namespace BFYOC.Models
{
    public class SalesEvent
    {
        public Header header;
        public SalesDetails [] details;       
        public string id;
    }

        
    public class Header
    {
        public string salesNumber;
        public DateTime dateTime;
        public string locationId;
        public string locationName;
        public string locationAddress;
        public string locationPostcode;
        public string totalCost;
        public string totalTax;
        public string receiptUrl = "";
    }

        
    public class SalesDetails
    {
        public string productId;
        public string productName;
        public string productDescription;
        public string totalTax;
        public string totalCost;
        public string quantity;
        public string unitCost;
    }
}