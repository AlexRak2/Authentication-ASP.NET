using System.ComponentModel.DataAnnotations;

namespace Authentication.Models
{
    public class EmailHistory
    {
        [Key]
        public int EmailId { get; set; }
        public string MailFrom { get; set; }
        public string MailTo { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public DateTime SentDate { get; set; }
    }
}
