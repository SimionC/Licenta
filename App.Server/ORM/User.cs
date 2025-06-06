using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace App.Server.ORM;

public partial class User
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public string Email { get; set; } = null!;

    public string Nume { get; set; } = null!;

    public string Prenume { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string? StudentId { get; set; }

    [Required]
    public int UserTypeId { get; set; }


    // Foreign Keys 
    [ForeignKey("UserTypeId")]
    public UserType UserType { get; set; } = null!;

}
