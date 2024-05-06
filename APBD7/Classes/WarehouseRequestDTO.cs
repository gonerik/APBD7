namespace APBD7.Classes;


using System.ComponentModel.DataAnnotations;

public record CreateWarehouseProductRequest(
    [Required] int IdProduct,
    [Required] int IdWarehouse,
    [Required] int Amount,
    [Required] DateTime CreatedAt
);