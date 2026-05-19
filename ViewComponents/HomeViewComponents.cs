using KinexusMockup.Models;
using Microsoft.AspNetCore.Mvc;

namespace KinexusMockup.ViewComponents;

public class HomeViewComponent : ViewComponent
{
    public IViewComponentResult Invoke()
    {
        var model = new HomeViewModel
        {
            HeroTitle = "Welcome To the Kinexus Bioinformatics Corporation website",
            HeroDescription = "Since 1999, we have continued to offer innovative services and products to the biomedical " +
            "research community to advance knowledge of cellular signalling systems to facilitate development " +
            "of improved disease diagnostics and therapeutics. Over 3,500 clients in academia and industry in over " +
            "40 countries have already utilized our services. We invite you to explore our website and learn the " +
            "many innovative ways that we can assist you in your research endeavours. We strive to be one of your " +
            "preferred commercial partners for systems proteomics research."
        };

        return View(model);
    }
}