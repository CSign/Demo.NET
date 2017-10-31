using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace CSign.Integration.Example.Models
{
    public class SignModel
    {
        public SignModel() {
            SessionId = Guid.NewGuid();
        }

        [Required(ErrorMessage = "{0} is required")]
        [Display(Name = "Title")]
        public string   Title        { get; set; }

        public Guid     SessionId   { get; set; }

        [Required(ErrorMessage = "{0} is required")]
        [Display(Name = "Description")]
        public string Description { get; set; }
    }
}