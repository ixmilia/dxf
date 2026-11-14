namespace IxMilia.Dxf
{
    public interface IDxfItem
    {
        DxfHandle Handle { get; set; }
        IDxfItem? Owner { get; }
        DxfHandle OwnerHandle { get; set; }
        void SetOwner(IDxfItem owner);
    }
}
