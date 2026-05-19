using KinexusMockup.Models;
using Microsoft.AspNetCore.Mvc;

namespace KinexusMockup.ViewComponents;

public class HomeViewComponent : ViewComponent
{
    public IViewComponentResult Invoke()
    {
        var model = new HomeViewModel
        {
            WelcomeMessage = """
            Since 1999, we have continued to offer innovative services and products to the biomedical 
            research community to advance knowledge of cellular signalling systems to facilitate development 
            of improved disease diagnostics and therapeutics. Over 3,500 clients in academia and industry 
            in over 40 countries have already utilized our services. We invite you to explore our website 
            and learn the many innovative ways that we can assist you in your research endeavours. 
            We strive to be one of your preferred commercial partners for systems proteomics research.
            """,

            SignetMessage = """
            Kinexus is pleased to develop and maintain these open-access knowledgebases to foster 
            the study of cell signalling systems and advance biomedical research in academia and industry 
            for diagnostic and therapeutic solutions to confront human diseasesKinexus is pleased to 
            develop and maintain these open-access knowledgebases to foster the study of cell signalling 
            systems and advance biomedical research in academia and industry for diagnostic and therapeutic 
            solutions to confront human diseases
            """,

            Knowledgebanks = new List<KnowledgebankItem>
            {
                new KnowledgebankItem
                {
                    Title = "PhosphoNET",
                    Description = """
                        is the world’s largest repository of information on over 950,000 known and 
                        predicted human phosphorylation sites, including their evolutionary conservation, and the known 
                        and predicted identities of protein kinases that may target these phosphosites.
                        """,
                    LinkUrl = "/phosphonet"
                },
                new KnowledgebankItem
                {
                    Title = "KinaseNET",
                    Description = """
                        features comprehensive information on over 530 human protein kinases, including their 
                        structures, regulation, substrates, tissue distribution, evolutionary conservation, sensitivities to compounds, 
                        and linkages to human diseases.
                        """,
                    LinkUrl = "/kinasenet"
                },
                new KnowledgebankItem
                {
                    Title = "KiNECTOR",
                    Description = """
                        features over direct 22,000 kinase-substrate relationships as well as indirect connections 
                        between kinases and phosphoproteins within four degrees of connectivity in signalling maps 
                        with direct links to other informative websites.
                        """,
                    LinkUrl = "/drugnet"
                },
                new KnowledgebankItem
                {
                    Title = "OncoNET",
                    Description = """
                        is a cancer protein-focused knowledgebase with data on the expression levels and mutations 
                        in about 3,000 human proteins that are linked to cancer in diverse human tissues and tumour cell lines.
                        """,
                    LinkUrl = "/onconet"
                },
                new KnowledgebankItem
                {
                    Title = "TranscriptoNET",
                    Description = """
                        features information on the mRNA expression levels from DNA microarray studies for over 
                        20,000 genes in about 600 types of human organs, tissues and cells from over 900 studies 
                        with 6,000 biological specimens deposited in the NCBI GEO webisite.
                        """,
                    LinkUrl = "/transcriptonet"
                },
                new KnowledgebankItem
                {
                    Title = "KinAtlas",
                    Description = """
                        combines data from Kinexus's TranscriptoNET and DrugKiNET websites with data from the 
                        EMBL STRING database to create customizable protein-protein and protein-drug interaction network maps.
                        """,
                    LinkUrl = "/kinatlas"
                },
                new KnowledgebankItem
                {
                    Title = "DrugKiNET",
                    Description = """
                        features comprehensive information on over 800 compounds that have tested in over 105,000 
                        experiments for inhibition of human protein kinases. It also provides over 200,000 predictions 
                        of off-target protein kinase compound interactions.
                        """,
                    LinkUrl = "/drugkinet"
                },
                new KnowledgebankItem
                {
                    Title = "DrugProNet",
                    Description = """
                        identifies the critical atomic interactions between over 2,000 drugs and their protein targets 
                        based on x-ray crystallographic studies and prediction of the effects of gene mutations on these interactions.
                        """,
                    LinkUrl = "/drugpronet"
                },
                new KnowledgebankItem
                {
                    Title = "KiNET-AM",
                    Description = """
                        features quantitative results from nearly 2,000 Kinex™ Antibody Microarray analyses with over 
                        1.5 million measurements of over 700 hundred different signalling proteins and phosphosites 
                        in diverse tissues and experimental model systems.
                        """,
                    LinkUrl = "/kinetam"
                }
            }
        };

        return View(model);
    }
}