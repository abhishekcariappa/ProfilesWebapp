using System;
using System.ComponentModel.DataAnnotations;

namespace ProfileManager.Models
{
    public class Jobs
    {
        public int ID { get; set; }
        public string Description { get; set; }

        [Display(Name = "Job Start Date")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime CreatedDate { get; set; }

    }
}

