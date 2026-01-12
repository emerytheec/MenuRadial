// Los controladores están en el mismo namespace

namespace Bender_Dios.MenuRadial.Components.Frame
{
    /// <summary>
    /// Factory por defecto que crea instancias directas de controladores
    /// Extraído de MRAgruparObjetos para cumplir con principio de responsabilidad única
    /// </summary>
    public class DefaultFrameControllerFactory : IFrameControllerFactory
    {
        public FrameObjectController CreateObjectController(FrameData frameData)
            => new FrameObjectController(frameData);
        
        public FrameMaterialController CreateMaterialController(FrameData frameData)
            => new FrameMaterialController(frameData);
        
        public FrameBlendshapeController CreateBlendshapeController(FrameData frameData)
            => new FrameBlendshapeController(frameData);
        
        public FramePreviewController CreatePreviewController(FrameObjectController objectController, 
            FrameMaterialController materialController, FrameBlendshapeController blendshapeController)
            => new FramePreviewController(objectController, materialController, blendshapeController);
    }
}