namespace LogicApp.Models;

public enum RequestCommand
{
    Other = 'o',
    GetItem = 'g',
    DoItemAction = 'a',
    CreateItem = 'c',
    UpdateItem = 'u',
    CreateOrUpdateItem = 'w',
    SoftDeleteItem = 's',
    HardDeleteItem = 'h'
}
