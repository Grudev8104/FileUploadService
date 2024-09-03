using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedLibrary.Models
{
    public class ProcessedFile
    {
        public int Id { get; set; }
        public string FileName { get; set; }
        public DateTime ProcessedDate { get; set; }
        public string FileContent { get; set; }  // JSON content
    }
}
