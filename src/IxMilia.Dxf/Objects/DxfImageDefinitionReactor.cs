namespace IxMilia.Dxf.Objects
{
    public partial class DxfImageDefinitionReactor
    {
        public IDxfItem AssociatedImage
        {
            get { return Owner; }
            set { SetOwner(value); }
        }
    }
}
