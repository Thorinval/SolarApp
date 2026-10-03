# Copilot Instructions

## General Guidelines
- Avant toute modification du fichier Excel d’origine, créer une sauvegarde de sécurité pour éviter toute perte/corruption.
- Dans ce dépôt, les traces consultables doivent être produites via un logging applicatif vers fichier, dans un sous-dossier Logs.
- Conserver Excel comme source pour certains relevés (notamment Linky), mais mettre à jour dans l’application les données provenant d’Atmoce Cloud ou qui en dépendent, y compris les données d’ensoleillement.

## Project-Specific Rules
- Les libellés de mois affichés dans l'application doivent commencer par une majuscule (ex. Septembre).
- Pour les navigations Playwright asynchrones d’AtmoceCloudBrowserService, la vérification de l’URL réelle doit être faite dans la boucle d’attente/polling et non uniquement avant d’y entrer.
- Pour le flux de connexion Atmoce Cloud, limiter les messages progress.Report visibles à 3 étapes maximum pour alléger l’UI.