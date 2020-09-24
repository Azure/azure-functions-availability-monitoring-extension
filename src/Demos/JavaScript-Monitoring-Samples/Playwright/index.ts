import { AzureFunction, Context, HttpRequest } from "@azure/functions"
const playwright = require("playwright-chromium");
const { AppInsightsContextListener } = require("./instrumentation")

const httpTrigger: AzureFunction = async function (context: Context, req: HttpRequest): Promise<void> {
    // playwright
    context.log("Started Playwright!");

    // initialize appInsightsListener to collect information about Playwright execution
    // set input parameter to:
    //   - 'AutoCollect' to collect screenshots after every action taken
    //   - 'OnFailure' to collect screenshots only for the failed actions
    //   - 'No' to skip the screenshots collection. Default value.
    const appInsightsListener = new AppInsightsContextListener('No');
    
    try {
        const browser = await playwright.chromium.launch();
        const browserContext = await browser.newContext();

        // Open new page
        const page = await browserContext.newPage();        

        // Go to https://ch-retailappz4rz6i.azurewebsites.net/
        await page.goto('https://ch-retailappz4rz6i.azurewebsites.net/');

        // Click text="Create New"
        await page.click('text="Create New"');
        // assert.equal(page.url(), 'https://ch-retailappz4rz6i.azurewebsites.net/ServiceTickets/Create');

        // Click input[name="Title"]
        await page.click('input[name="Title"]');

        // Fill input[name="Title"]
        await page.fill('input[name="Title"]', 'test ticket');

        // Click input[type="submit"]
        await page.click('input[type="submit"]');
        // assert.equal(page.url(), 'https://ch-retailappz4rz6i.azurewebsites.net/ServiceTickets/Details/1036');

        // Click text="Delete"
        await page.click('text="DeleteWrong"');

        // assert.equal(page.url(), 'https://ch-retailappz4rz6i.azurewebsites.net/ServiceTickets/Delete/1036');

        // Click input[type="submit"]
        await page.click('input[type="submit"]');
        // assert.equal(page.url(), 'https://ch-retailappz4rz6i.azurewebsites.net/ServiceTickets');

        // Close page
        await page.close();
    } catch (e) {
        context.log(e.message);
    }

    // Serialize collected data into the response
    context.res = appInsightsListener.serializeData()
};


export default httpTrigger;