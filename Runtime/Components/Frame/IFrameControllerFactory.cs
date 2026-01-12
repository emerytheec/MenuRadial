// Los controladores están en el mismo namespace

namespace Bender_Dios.MenuRadial.Components.Frame
{
    /// <summary>
    /// Factory para crear controladores de frame (permite inyección de dependencias)
    /// Extraído de MRAgruparObjetos para cumplir con principio de responsabilidad única
    /// </summary>
    public interface IFrameControllerFactory
    {
        FrameObjectController CreateObjectController(FrameData frameData);
        FrameMaterialController CreateMaterialController(FrameData frameData);
        FrameBlendshapeController CreateBlendshapeController(FrameData frameData);
        FramePreviewController CreatePreviewController(FrameObjectController objectController, 
            FrameMaterialController materialController, FrameBlendshapeController blendshapeController);
    }
}