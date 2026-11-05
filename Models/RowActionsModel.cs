namespace BioGamaEcuador.Models;

public sealed record RowActionsModel(int Id, bool AdminOnlyEdit = false,
    bool Tooltips = false, bool ViewFirst = false);
