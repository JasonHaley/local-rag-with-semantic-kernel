This is the first part of my [eShopSupport Series](/2024/08/23/introducing-eshopsupport-series/) which looks into the details of the [eShopSupport](https://github.com/dotnet/eshopsupport) GitHub repository.

# DataGenerator Project

The [DataGenerator](https://github.com/dotnet/eShopSupport/tree/main/seeddata/DataGenerator) is a console application that will generate multiple types of seed data files for loading into the application or to use when evaluating the question answering functionality. However, **you don't have to use the DataGenerator** - there are two sets of generated files provided in the github repo: [dev](https://github.com/dotnet/eShopSupport/tree/main/seeddata/dev) and [test](https://github.com/dotnet/eShopSupport/tree/main/seeddata/test). 

![Files](/img/2024-09-02_img1.jpg)

In this entry I will provide some detail of what the DataGenerator application does, some interesting things about it and a few things I'd like to change.

## What does it do?

This project is introduced about 16 minutes into Steve Sanderson's talk [How to add genuinely useful AI to your webapp (not just chatbots)](https://www.youtube.com/watch?v=TSNAvFJoP4M&t=960s), where he quickly walks through some of the files it generates. Later in the video, he gets to the support ticket application most of this seed data is for.

The goal of this project is to generate a few types of seed data for being able to demonstrate the functionality highlighted in Steve Sanderson't talk:

* Seed data for the support ticket system
* A bunch of pdfs for unstructured data
* Question/answer pairs for evaluating the chat functionality

Since the console application has a singular purpose (generate seed data files), all the important logic is driven from the [Program.cs](https://github.com/dotnet/eShopSupport/blob/main/seeddata/DataGenerator/Program.cs) file:
```c#
var categories = await new CategoryGenerator(services).GenerateAsync();
Console.WriteLine($"Got {categories.Count} categories");

var products = await new ProductGenerator(categories, services).GenerateAsync();
Console.WriteLine($"Got {products.Count} products");

var manualTocs = await new ManualTocGenerator(categories, products, services).GenerateAsync();
Console.WriteLine($"Got {manualTocs.Count} manual TOCs");

var manuals = await new ManualGenerator(categories, products, manualTocs, services).GenerateAsync();
Console.WriteLine($"Got {manuals.Count} manuals");

var manualPdfs = await new ManualPdfConverter(products, manuals).ConvertAsync();
Console.WriteLine($"Got {manualPdfs.Count} PDFs");

var tickets = await new TicketGenerator(products, categories, manuals, services).GenerateAsync();
Console.WriteLine($"Got {tickets.Count} tickets");

var ticketThreads = await new TicketThreadGenerator(tickets, products, manuals, services).GenerateAsync();
Console.WriteLine($"Got {ticketThreads.Count} threads");

var summarizedThreads = await new TicketSummaryGenerator(products, ticketThreads, services).GenerateAsync();
Console.WriteLine($"Got {summarizedThreads.Count} thread summaries");

var evalQuestions = await new EvalQuestionGenerator(products, categories, manuals, services).GenerateAsync();
Console.WriteLine($"Got {evalQuestions.Count} evaluation questions");
```

When you run this project, the data generation happens in the following sequence in order to create the things that are dependent first (ie. product generation depends on the categories, etc.):

1. Categories
2. Products
3. Product manual table of contents
4. Full manuals (markdown)
5. Pdf versions of the manuals
6. Support tickets
7. Support ticket threads
8. Support ticket summaries
9. Evaluation question/answer pairs

Once the project completes, all generated files will be in the `/output` directory:

![Output Files](/img/2024-09-02_img3.jpg)

### Categories

[CategoryGenerator](https://github.com/dotnet/eShopSupport/blob/main/seeddata/DataGenerator/Generators/CategoryGenerator.cs) has the batch sizes and prompt used to have the LLM create categories. Here is an example of the prompt:

```text
Generate 25 product category names for an online retailer
of high-tech outdoor adventure goods and related clothing/electronics/etc.
Each category name is a single descriptive term, so it does not use the word 'and'.
Category names should be interesting and novel, e.g., "Mountain Unicycles", "AI Boots",
or "High-volume Water Filtration Plants", not simply "Tents".
This retailer sells relatively technical products.

Each category has a list of up to 8 brand names that make products in that category. All brand names are
purely fictional. Brand names are usually multiple words with spaces and/or special characters, e.g.
"Orange Gear", "Aqua Tech US", "Livewell", "E & K", "JAXⓇ".
Many brand names are used in multiple categories. Some categories have only 2 brands.
            
The response should be a JSON object of the form { "categories": [{"name":"Tents", "brands":["Rosewood", "Summit Kings"]}, ...] }
```

Unless duplicate categories are generated, it should only take two calls to the LLM to generate the 50 categories.

The application creates 50 files containing category and brand data under the `/output/categories` directory. Here is and example of a category:
```json
{
  "categoryId": 1,
  "name": "Solar-Powered Backpacks",
  "brands": [
    "SunStride Gear",
    "Ecocharge",
    "Photon Packs",
    "Suntrail",
    "Lumen Load",
    "Daylight Haulers",
    "Brightpack",
    "Solar Journey"
  ]
}
```

### Products

[ProductGenerator](https://github.com/dotnet/eShopSupport/blob/main/seeddata/DataGenerator/Generators/ProductGenerator.cs) has number of products (200) to generate and batch size (5) to use when calling the LLM to generate product descriptions. The prompt grabs 5 random (batch size) items from the generated categories and adds them into the prompt. Here is an example of a prompt sent to the LLM:

```text
Write list of 5 products for an online retailer
of outdoor adventure goods and related electronics, clothing, and homeware. There is a focus on high-tech products. They match the following category/brand pairs:
- product 1: category Satellite Communicators, brand: StarLink Devices
- product 2: category UV-Protective Clothing, brand: Heatdefend
- product 3: category Bio-sensing Gloves, brand: Biomitt
- product 4: category Geothermal Cookware, brand: Earth Heat
- product 5: category Biometric Locks, brand: Fingerguard

Model names are up to 50 characters long, but usually shorter. Sometimes they include numbers, specs, or product codes.
Example model names: "iGPS 220c 64GB", "Nomad Camping Stove", "UX Polarized Sunglasses (Womens)", "40L Backpack, Green"
Do not repeat the brand name in the model name.

The description is up to 200 characters long and is the marketing text that will appear on the product page.
Include the key features and selling points.

The result should be JSON form { "products": [{ "id": 1, "brand": "string", "model": "string", "description": "string", "price": 123.45 }] }
```

There will be 40 calls to the LLM to create 200 product description files.

Once complete, 200 files containing sample product information are generated under the `/output/products` directory. Here is an example: 
```json
{
  "categoryId": 40,
  "productId": 113,
  "brand": "Float Bag",
  "model": "UltraLite AG Sleeping Bag",
  "description": "Experience weightlessness with the UltraLite AG. Anti-gravity technology ensures unparalleled comfort and warmth on any terrain.",
  "price": 129.99
}
```

### Table of contents for a product manual

Pdf product manuals are created in 3 steps:
1. Create a table of contents
2. Create content for each section in each table of contents and merge into a single file
3. Covert the markdown version to pdf

The first step is completed by [ManualTocGenerator](https://github.com/dotnet/eShopSupport/blob/main/seeddata/DataGenerator/Generators/ManualTocGenerator.cs) which generates a table of contents file for a product manual, for every product that is passed in from the ProductGenerator.

There are 8 writing styles the generator randomly selects from to use in the prompt along with a category, brand, product model and product description. Here is an example of the prompt passed to the LLM:

```text
Write a suggested table of contents for the user manual for the following product:

Category: Solar-powered Stoves
Brand: Sun Cooks
Product name: Eco-Friendly Solar Stove SPX100
Overview: Harness the power of the sun with this lightweight, portable solar stove. Perfect for eco-conscious adventurers.

The manual MUST be written in the following style: trying to be cool and hip, with lots of emojis
The table of contents MUST follow that style, even if it makes the manual useless to users.
            
The response should be a JSON object of the form
{
    "sections": [
        {
            "title": "...",
            "subsections": [
                {
                    "title": "...",
                    "subsections": [...]
                },
                ...
            ]
        },
        ...
    ]
}

Subsections can be nested up to 3 levels deep. Most sections have no subsections. Only use subsections for the most important, longest sections.
```

There will be 200 calls to the LLM to create 200 table of content files.

The generator creates 200 files containing a table of contents for a manual of each of the 200 products created under the `/output/manuals/toc` directory. An shortened example:
```json
{
  "productId": 101,
  "manualStyle": "normal",
  "sections": [
    {
      "siblingIndex": 1,
      "title": "Introduction",
      "subsections": []
    },
    {
      "siblingIndex": 2,
      "title": "Safety Precautions",
      "subsections": [
        {
          "siblingIndex": 1,
          "title": "General Safety",
          "subsections": []
        },
        {
          "siblingIndex": 2,
          "title": "Solar Heat Safety",
          "subsections": []
        },
        {
          "siblingIndex": 3,
          "title": "First Aid",
          "subsections": []
        }
      ]
    }
    /// more sections
  ]
}
```

### Product manuals

The second step in creating the product manuals is done by [ManualGenerator](https://github.com/dotnet/eShopSupport/blob/main/seeddata/DataGenerator/Generators/ManualGenerator.cs), which takes all the table of content files and populates every section in order to create a full product manual.

The prompt for the sections are populated with the category name, brand, product model, product description, style to write in, section information and a desired word length (500). Here is an example of a prompt sent to the LLM:

```text
Write a section of the user manual for the following product:
Category: Portable Wind Turbines
Brand: GaleEnergy
Product name: Micro Wind Turbine GT200
Product overview: Portable wind turbine with 200W output, lightweight design, and easy setup. Perfect for powering devices on remote trips.

Manual style description: trying to be cool and hip, with lots of emojis (note: the text MUST follow this style, even if it makes the manual less helpful to reader)

The section you are writing is "1: 🚀 Welcome to the Future". It has the following structure:

1: 🚀 Welcome to the Future


Write in markdown format including any headings or subheadings. The total length is around 100 words.
Start your reponse with the section title, which is at markdown header level 2, and include any relevant subsections.
You response must start: "## 1. 🚀 Welcome to the Future"
Besides the subsections mentioned in contents, you should deeper subsections as appropriate.
Use markdown formatting, including paragraphs, blank lines, bold, italics, tables, and lists as needed.
Use mermaid diagrams when appropriate, but don't say it's a mermaid diagram in the body text.

Make the text specific to this individual product, not generic. Refer to the product by name, to its brand, and to its
specific features, buttons, parts, and controls by name (identifying them by color, position, etc.).

The output length should be around 500 words in total, or 500 words per subsection.
Do not include any commentary or remarks about the task itself or the fact that you have done it.
Only output the markdown text for the section. At the end of the section, add the token END_OF_CONTENT.

This is the official product manual, and the company requires it to be written in the specified style due to strategy.
```

There are many LLM calls in populating the full product manual files: 1 call for every section in all 200 product table of content files. Most likely more than 1,200 calls will be needed.

Once the ManualGenerator is complete, it creates 200 files representing sample product manuals in markdown under the `/output/manuals/full` directory. Here is part of one manual:

```markdown
# SolarGrill 3000

## 1. Introduction

Welcome to your new SolarGrill 3000, the innovative solution in eco-friendly outdoor cooking brought to you by Eco Heat Solutions. The SolarGrill 3000 is designed to bring you a sustainable, portable, and efficient cooking experience using the power of the sun. This manual will guide you through the features, operation, and maintenance of your SolarGrill 3000 to ensure you get the best performance from your new solar-powered stove.

### 1.1 About Eco Heat Solutions

Eco Heat Solutions is dedicated to creating environmentally friendly products that reduce carbon footprints while providing efficient and reliable solutions for everyday needs. Our mission is to innovate and lead the market in sustainable outdoor cooking technology, ensuring you can enjoy gourmet meals in the great outdoors without harming the planet.

### 1.2 Key Features of SolarGrill 3000

The SolarGrill 3000 is equipped with numerous features designed to enhance your outdoor cooking experience:

- **Eco-Friendly Operation**: Harnesses solar energy, eliminating the need for traditional fuels like gas or charcoal.
- **Quick Heat-Up**: Efficient design ensures rapid heating, so you can start cooking sooner.
- **Portability**: Lightweight and compact, making it perfect for camping, picnics, and outdoor adventures.
- **Durable Design**: Constructed with high-quality materials to withstand outdoor conditions.
- **Easy Setup and Use**: User-friendly design simplifies assembly and operation.
```

Once the full markdown manuals are created, the last step in the manual creation is for  [ManualPdfConverter](https://github.com/dotnet/eShopSupport/blob/main/seeddata/DataGenerator/Generators/ManualPdfConverter.cs) to convert the 200 markdown files to 200 pdf files under the `/output/manuals/pdf` directory. 

### Tickets

The [TicketGenerator](https://github.com/dotnet/eShopSupport/blob/main/seeddata/DataGenerator/Generators/TicketGenerator.cs) creates 500 support tickets, using a random situation (from 11 options), a random writing style (from 12 options), along with product, category, random sections of manual markdown to create a prompt that will generate a support ticket. Here is an example of a prompt that was sent to the LLM:

```text
You are creating test data for a customer support ticketing system.
Write a message by a customer who has purchased, or is considering purchasing, the following:

Product: BreezeGen 500
Brand: WindyPower
Category: Portable Wind Turbines
Description: Stay powered anywhere with BreezeGen 500. This portable wind turbine is compact, easy to set up, and perfect for charging devices in remote locations.
Random extract from manual: <extract>- **Grounding**: Properly ground the BreezeGen 500 using the provided grounding kit to avoid electrical shocks.
### 3.3 Operation
- **Weather Conditions**: Do not operate the BreezeGen 500 in extreme weather conditions, including thunderstorms, high winds exceeding 50 km/h, or when there is a risk of lightning.
- **Moving Parts**: Avoid contact with the rotor blades while the BreezeGen 500 is operational. The blades spin at high speed and can cause serious injury.
- **Noise Awareness**: Be aware that the BreezeGen 500 can generate noise during operation. Position the turbine in a location where noise will not disturb you or others.
### 3.4 Maintenance and Repairs
- **Scheduled Inspections**: Regularly inspect the BreezeGen 500 for any signs of wear, damage, or loose components. 
- **Disconnecting Power**: Before performing maintenance, ensure the BreezeGen 500 is disconnected from any power sources.
- **Replacement Parts**: Use only WindyPower-approved components for repairs or replacements. Unauthorized parts may compromise safety and performance.
### 3.5 Electrical Safety
- **Avoid Water**: Keep the BreezeGen 500 and its electrical components dry at all times to prevent electrical shocks or malfunctions.
- **Cables**: Ensure all cables are in good condition and properly connected. Damaged or frayed cables should be replaced immediately.
- **Emergency Shutoff**: Familiarize yourself with the location and operation of the emergency shutoff switch, located on the base of the turbine.
</extract>

The situation is: asking about a particular usage scenario before purchase
If applicable, they can ask for a refund/replacement/repair. However in most cases they
are asking for information or help with a problem.

The customer writes in the following style: frustrated and angry

Create a name for the author, writing the message as if you are that person. The customer name
should be fictional and random, and not based on the support enquiry itself. Do not use cliched
or stereotypical names.

Where possible, the message should refer to something specific about this product such as a feature
mentioned in its description or a fact mentioned in the manual (but the customer does not refer
to having read the manual).

The message length may be anything from very brief (around 10 words) to very long (around 200 words).
Use blank lines for paragraphs if needed.

The result should be JSON form { "customerFullName": "string", "message": "string" }.
```

There are batches of 10 request running in order to generate these tickets, however that still means 10 calls to the LLM. So in order to get 500 tickets, you need 500 calls.

Once the TicketGenerator completes, there will be 500 files under the `/output/tickets/enquiries` directory. Here is an example of a generated ticket:

```json
{
  "ticketId": 1,
  "productId": 109,
  "customerFullName": "Jordan Culpepper",
  "message": "I am absolutely furious right now! I need clear answers before I even think about purchasing your BreezeGen 500. I plan on using it during my camping trips in remote locations, but I need to know how this thing can handle different weather conditions. You mention it\u0027s supposed to be used \u0027anywhere,\u0027 but then there are warnings about thunderstorms, high winds, and lightning risks. What kind of a \u0027portable\u0027 wind turbine is this if I have to constantly worry about weather conditions? And don\u0027t get me started on the noise - is it so loud that it will ruin the peace and quiet of my camping experience? I\u0027m not wasting my money on a product that can\u0027t deliver on its promises!",
  "customerSituation": "asking about a particular usage scenario before purchase",
  "customerStyle": "frustrated and angry"
}
```

### Ticket threads and summaries

In order to create seed data for the back and forth of customers and assistant in support tickets the [TicketThreadGenerator](https://github.com/dotnet/eShopSupport/blob/main/seeddata/DataGenerator/Generators/TicketThreadGenerator.cs) creates messages for assistants and customers given information about the product, ticket and previous messages in the thread. 

There are two types of prompts (one for the assistant point of view and another for the customer point of view). Here is an example of a prompt used from the customer point of view:

```text
You are generating test data for a customer support ticketing system. There is an open ticket as follows:
        
Product: BreezeGen 500
Brand: WindyPower
Customer name: Jordan Bentley

The message log so far is:

<message role="Customer">hey idk what im doin w this BreezeGen 500 thing. tried settin it up but not sure if the blades r on right? also how do u even clean it? more importantly, is it ok 2 use it in the rain?? pls help</message>
<message role="Assistant">Hello! Let's get your BreezeGen 500 up and running smoothly. Here's what you need to know:

1. **Blade Installation**: According to the manual, to install the blades, align the rotor with the designated slots on the turbine unit and press firmly until you hear a click, indicating that the rotor is securely in place. Then, insert each rotor blade into the designated slots on the rotor.

2. **Cleaning Instructions**: The manual suggests cleaning the blades and the main body with a damp cloth to remove debris. Regular inspections for wear and tear and ensuring all screws and bolts are tight are also recommended.

3. **Usage in Rain**: The BreezeGen 500 should not be operated in extreme weather conditions, including thunderstorms, high winds exceeding 50 km/h, or when there is a risk of lightning.

If you need further assistance, feel free to ask!

Best regards,
AdventureWorks Support</message>


Generate the next reply from the customer. You may do any of:

- Supply more information as requested by the support agent
- Say you did what the support agent suggested and whether or not it worked
- Confirm that your enquiry is now resolved and you accept the resolution
- Complain about the resolution
- Say you need more information

Write as if you are the customer. This customer ALWAYS writes in the following style: extremely brief and abbreviated, by a teenager typing on a phone while distracted by another task.

Respond in the following JSON format: { "message": "string", "shouldClose": bool }.
Indicate that the ticket should be closed if, as the customer, you feel the ticket is resolved (whether or not you are satisfied).
```

There are 500 tickets to create threads for and on average there will be 2 - 4 messages in those threads, this means it will take between 1,000 and 2,000 calls to the LLM to generate all 500 threads.

Here is an example thread file:
```json
{
  "ticketId": 10,
  "productId": 125,
  "customerFullName": "Alexis Rowen",
  "messages": [
    {
      "messageId": 1,
      "authorRole": 0,
      "text": "Hey, so I got the Sunaid SolarCharger Pro 50W but it\u0027s not working right. I put it in the sun like forever, and the light turns red. What\u0027s that about? I\u0027m also confused by the colors it shows, really inconsistent. I need to charge my stuff quickly for an upcoming trip and this isn\u2019t doing it. Can I get a fix or replacement or something?"
    },
    {
      "messageId": 2,
      "authorRole": 1,
      "text": "According to the manual, the red light on your SolarCharger Pro 50W means that it is \u0027angry\u0027 and not charging. Please ensure the device is positioned in direct sunlight and try again. For inconsistent light colors, you might want to check if the solar panels are not obstructed or dirty.\n\nIf the issue persists, you can return the defective product within 1 year of purchase for a refund, as per our policy. You can initiate a return at: https://northernmountains.example.com/support/returns.\n\nIf you need further assistance, please let us know!"
    },
    {
      "messageId": 3,
      "authorRole": 0,
      "text": "Yo, I tried that, still red light. Cleaned it too, no help. Need replacement asap, trip is in 3 days. Can\u0027t wait for a refund."
    },
    {
      "messageId": 4,
      "authorRole": 1,
      "text": "According to the manual, there is **NO WARRANTY** for the SolarCharger Pro 50W. If the red light issue persists despite positioning it correctly and ensuring the panels are clean, we regret to inform you that we cannot offer a replacement under warranty.\n\nYou might wish to check the USB cable as well to ensure it\u2019s not the source of the problem. If issues continue, the manual humorously suggests finding creative uses for the product, but we understand that\u0027s not ideal for your upcoming trip.\n\nFor urgent needs, purchasing a new unit might be the quickest solution, considering your deadline.\n\nWe apologize for any inconvenience this has caused, and thank you for understanding.\n\nIs there anything else we can assist you with?"
    },
    {
      "messageId": 5,
      "authorRole": 0,
      "text": "Wow rly? No warranty and buy another? Ugh. Trip is in 3 days but guess I don\u0027t hv a choice. Won\u0027t be recommending u guys. Thx anyway."
    }
  ],
  "shortSummary": "Red light issue, no warranty",
  "longSummary": "Customer reports red light issue on SolarCharger Pro 50W. Already cleaned and positioned in sunlight. No warranty; advised to purchase new unit. Customer very unhappy.",
  "customerSatisfaction": 1,
  "ticketStatus": "Open",
  "ticketType": "Complaint"
}
```

The next step is to populate the summaries for those ticket threads using the  [TicketSummaryGenerator](https://github.com/dotnet/eShopSupport/blob/main/seeddata/DataGenerator/Generators/TicketSummaryGenerator.cs). Passing the products and threads allows the generator to use the product model, brand, customer name, thread messages and list of satisfaction scores to use in creating a long summary, short summary, ten word summary of the customer messages, satisfaction rating, ticket status and ticket type. 
An example of a prompt used in generating the summaries:

```text
You are part of a customer support ticketing system.
Your job is to write brief summaries of customer support interactions. This is to help support agents
understand the context quickly so they can help the customer efficiently.

Here are details of a support ticket.

Product: HydraBoost Electrolyte Bottle
Brand: Power Gulp
Customer name: Jordan Fairchild

The message log so far is:

<message role="Customer">Dear Power Gulp Customer Support, I'm writing because I recently purchased your HydraBoost Electrolyte Bottle and I need some urgent information. I noticed that the bottle is supposed to have a leak-proof design, but I am extremely concerned about the way the cap twists on and off. Every single time I twist the lid, I feel like it’s not locking properly. The mechanism seems flimsy and cannot possibly be leak-proof as advertised. This is unacceptable for a product that claims to be perfect for outdoor activities. I demand a clear explanation on how exactly this should work and why my experience doesn’t match your marketing. Also, I'd like to know if there is some special technique I need to use to ensure it's locked in place correctly. If this issue cannot be resolved promptly, I will be expecting a full refund or replacement. I'm awaiting your immediate response. Regards, Jordan Fairchild</message>

Write these summaries:

1. A longer summary that is up to 30 words long, condensing as much distinctive information
as possible. Do NOT repeat the customer or product name, since this is known anyway.
Try to include what SPECIFIC questions/info were given, not just stating in general that questions/info were given.
Always cite specifics of the questions or answers. For example, if there is pending question, summarize it in a few words.
FOCUS ON THE CURRENT STATUS AND WHAT KIND OF RESPONSE (IF ANY) WOULD BE MOST USEFUL FROM THE NEXT SUPPORT AGENT.

2. A shorter summary that is up to 8 words long. This functions as a title for the ticket,
so the goal is to distinguish what's unique about this ticket.

3. A 10-word summary of the latest thing the CUSTOMER has said, ignoring any agent messages. Then, based
ONLY on that, score the customer's satisfaction using one of the following phrases ranked from worst to best:
AbsolutelyFurious, VeryUnhappy, Unhappy, Disappointed, Indifferent, Pleased, Happy, Delighted, UnspeakablyThrilled.
Pay particular attention to the TONE of the customer's messages, as we are most interested in their emotional state.

Both summaries will only be seen by customer support agents.

Respond as JSON in the following form: {
    "longSummary": "string",
    "shortSummary": "string",
    "tenWordsSummarizingOnlyWhatCustomerSaid": "string",
    "customerSatisfaction": "string",
    "ticketStatus": "Open"|"Closed",
    "ticketType": "Question"|"Idea"|"Complaint"|"Returns"
}

ticketStatus should be Open if there is some remaining work for support agents to handle, otherwise Closed.
ticketType must be one of the specified values best matching the ticket. Do not use any other value except the specified ones.
```

### Evaluation question/answer pairs

The last type of seed data this project generates is question/answer pairs to use when running evaluations on the system. The [EvalQuestionGenerator](https://github.com/dotnet/eShopSupport/blob/main/seeddata/DataGenerator/Generators/EvalQuestionGenerator.cs) uses a random product, its category, random extract from the product's manual and indicator of whether the question should be written by the agent or customer to populate a prompt. Below is an example of one of these prompts:

```text
There is an AI system used by customer support agents working for an online retailer.
The AI system is used to help agents answer customer questions.

Your task is to write question/answer pairs that will be used to evaluate the
performance of that AI system. All the questions you write will be about actual products
sold by that retailer, based on information from the product catalog and manuals. The
questions should plausibly represent what customers and support agents will ask.

In this case, you are to write a question/answer pair based on the following context:

<product_name>ThermoTent Pro 3000</product_name>
<brand>CoolCampers</brand>
<category>Climate-controlled Tents</category>
<extract_from_manual>This paper offers a comprehensive analysis of energy efficiency as it pertains to temporary shelters, with direct applicability to the ThermoTent Pro 3000. Energy consumption metrics are quantified, facilitating empirical measurement using IEC 62301.
#### 10.2.4. "Durability Testing of Lightweight Fabric Structures"
Patel, N. R. (2017). Durability Testing of Lightweight Fabric Structures. *Structural Engineering Review*, 45(1), 88-104.
Patel’s work is instrumental in establishing testing criteria for the ThermoTent Pro 3000’s fabric durability. Employ MIL-STD-810H methodologies for environmental testing, ensuring fabric integrity under variant conditions.
#### 10.2.5. "Electrical Systems Integration in Recreational Products"
Gupta, S. K. (2019). Electrical Systems Integration in Recreational Products. *Journal of Recreational Engineering*, 34(5), 567-580.
Gupta explores the intricate process of integrating electrical systems into recreational products, which is highly relevant to the ThermoTent Pro 3000. Calibration and diagnostic methods align with the standards detailed in IEC 61557-2.

## 11. 11. Appendix
### 11.11.1 Acronyms and Abbreviations
| Acronym | Definition                                   |
|---------|----------------------------------------------|
| ASTM    | American Society for Testing and Materials   |
| ISO     | International Organization for Standardization|
| R-Value | Thermal Resistance Value                     |
| HVAC    | Heating, Ventilation, and Air Conditioning   |
</extract_from_manual>

Questions are one of the following types:
 - A pre-purchase question to help a customer who wants to know about the product
   features, suitability for particular use cases, or other objective facts
 - A post-purchase question to help a customer resolve an issue or understand how to
   use the product

You must select an OBJECTIVE FACT from the product manual and write a question to which
that fact is the answer. Only select facts that are distinctive about this specific product,
not generic information about any product or warranty terms.

Always follow these style guidelines:
 - Questions are short, typically 3-6 words, and are not always full sentences. They may look
like search queries or things typed in a hurry by a support agent. They are not polite or
verbose, since they are addressed to a machine.
Example questions might be "weight", "what are the dimensions", "how to shut down",
"can use on pets?", "what accessories does it come with?"
 - Answers are short, typically a single brief sentence of 1-10 words. Never use more than
   20 words for an answer.
 - The "verbatim_quote_from_manual" is 3-6 words taken EXACTLY from the manual which are
   the factual basis for the question and asnwer.
 - If the provided context does not contain a suitable fact, set all the response properties
   to null or empty strings.

Respond as JSON in the following form: {
    "question": "string",
    "answer": "string",
    "verbatimQuoteFromManual": "string"
}
```

There will be a single call to the LLM for each 500 question/answer pair requests.

When the generator is complete, there will be 500 files under the `/output/evalquestions` directory. Here is an example of a generated question/answer pair:
```json
{
  "questionId": 106,
  "productId": 117,
  "question": "battery removal risks?",
  "answer": "Severe chemical burns.",
  "verbatimQuoteFromManual": "severe chemical burns"
}
```

## Dependencies

The DataGenerator project uses a few dependencies to provide functionality with the file generation:

* [Markdown2Pdf](https://github.com/Flayms/Markdown2Pdf/wiki/Markdown2Pdf.Markdown2PdfConverter) - used for converting the genertated markdown product manual files into pdf files. This uses chrome behind the scene.
* [Microsoft.SemanticKernel](https://github.com/microsoft/semantic-kernel) - used mostly for the [IChatCompletionService](https://github.com/microsoft/semantic-kernel/blob/6009370c239dfe7064e36f89df70ccf5a3b5e7e5/dotnet/src/SemanticKernel.Abstractions/AI/ChatCompletion/IChatCompletionService.cs#L14) and [Azure OpenAI connector's implementation of it](https://github.com/microsoft/semantic-kernel/blob/6009370c239dfe7064e36f89df70ccf5a3b5e7e5/dotnet/src/Connectors/Connectors.OpenAI/ChatCompletion/AzureOpenAIChatCompletionService.cs) to make calls to the LLM.
* [SmartComponents.LocalEmbeddings.SemanticKernel](https://github.com/dotnet-smartcomponents/smartcomponents/blob/main/docs/local-embeddings.md) - used to find related content in the manuals. This uses the [Onnx runtime](https://onnxruntime.ai/docs/get-started/with-csharp.html) behind the scene with the model.onnx and vocab.txt file you can find in the LocalEmbeddingsModel directory after you build the project.

![Onyx Files](/img/2024-09-02_img2.jpg)

## How to set it up

First you'll need to create an Azure OpenAI resource and a chat deployment. Then as described in the [appsettings.json](https://github.com/dotnet/eShopSupport/blob/main/seeddata/DataGenerator/appsettings.json) file, you need to create a file named `appsetttings.Local.json` and add your model's connection information to it:

```json
{
  "ConnectionStrings": {
    "chatcompletion": "Endpoint=https://<resource name>.openai.azure.com/;Key=<key>;Deployment=<chat deployment name>"
  }
}
```

In case you didn't read through the sections above be aware running this project **will make a lot of calls to the LLM** - this also means it takes awhile to generate all the files. 

## Points of Interest

These are some points in the code base that I found interesting and will be revisiting when writing my own data generators.

### Using a "normal" connection string to hold the model configuration and extension method

This is the first code base I've seen a connection string used to hold all the model connection information. I will be using this idea on my projects going forward. It makes more sense than the multiple app settings needed and is more flexible when you support multiple configurations (like my [PropertyGraphRAG app that supports Azure OpenAI and OpenAI](https://github.com/JasonHaley/semantic-kernel-getting-started/blob/main/samples/demos/PropertyGraphRAG/PropertyGraphRAG/appsettings.json))

### Usage of local embeddings

The [TicketThreadGenerator](https://github.com/dotnet/eShopSupport/blob/main/seeddata/DataGenerator/Generators/TicketThreadGenerator.cs) uses the `LocalTextEmbeddingGenerationService` when setting up the `AssistantTools` call to generate and utilize embeddings locally without needing to call OpenAI or other embeddings API.

```c#
private readonly ITextEmbeddingGenerationService embedder = new LocalTextEmbeddingGenerationService();

//... 

private class AssistantTools(ITextEmbeddingGenerationService embedder, Manual manual)
{
    [KernelFunction, Description("Searches for information in the product's user manual.")]
    public async Task<string> SearchUserManualAsync([Description("text to look for in user manual")] string query)
    {
        // Obviously it would be more performant to chunk and embed each manual only once, but this is simpler for now
        var chunks = TextChunker.SplitPlainTextParagraphs([manual.MarkdownText], 100);
        var embeddings = await embedder.GenerateEmbeddingsAsync(chunks);
        var candidates = chunks.Zip(embeddings);
        var queryEmbedding = await embedder.GenerateEmbeddingAsync(query);

        var closest = candidates
            .Select(c => new { Text = c.First, Similarity = TensorPrimitives.CosineSimilarity(c.Second.Span, queryEmbedding.Span) })
            .OrderByDescending(c => c.Similarity)
            .Take(3)
            .Where(c => c.Similarity > 0.6f)
            .ToList();

        if (closest.Any())
        {
            return string.Join(Environment.NewLine, closest.Select(c => $"<snippet_from_manual>{c.Text}</snippet_from_manual>"));
        }
        else
        {
            return "The manual contains no relevant information about this";
        }
    }
}
```

### Code optimized for parallelism

There are several approaches used to make the all the calls to the LLM in parallel in this code base as well as usage of await/async that help optimize for parallelism.

The [GeneratorBase](https://github.com/dotnet/eShopSupport/blob/main/seeddata/DataGenerator/Generators/GeneratorBase.cs) `GenerateAsync()` method is a good place to start looking and then look at the `GenerateCoreAsync()` implementations by the following derived classes:
* CategoryGenerator.cs - shows the usages of [yield return](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/statements/yield)
* ProductGeneratior.cs - shows the usage of the `MapParallel()` in the GeneratorBase.cs and its usage of [Channels](https://learn.microsoft.com/en-us/dotnet/api/system.threading.channels?view=net-8.0) and [Parallel.ForEachASync](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.parallel?view=net-8.0).
* TicketGenerator.cs - shows usage of [`Task.WhenAll()`](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task?view=net-8.0)
* EvalQuestionGenerator.cs - uses [Channels](https://learn.microsoft.com/en-us/dotnet/api/system.threading.channels?view=net-8.0) in a slightly different way than the `MapParallel()`

## Thoughts on Improvements

When running the DataGenerator the past week, I've noticed a few things I'd like to add to it or change.

### Handle invalid JSON responses

Every now and then I would get an invalid JSON exception:

![JSON Error](/img/2024-09-02_img5.png)

I'd like to add the handling of this issue. I believe that OpenAI returned invalid JSON, so the only really option would be to make the call again from scratch. There are retries already in the system, so I'm not 100% sure anything else needs done - but it did stop me a few times.

### Handle content filtering

At one point I had a content filter trigger (self harm = true), it would be nice to handle this scenario coming back from the LLM.

### Add ability to view trace messages

One thing I've found super useful when learning Semantic Kernel is turning on trace logging to the Console. It would be nice to have that already configured and ready to turn on.

### Add ability to use other models besides just Azure OpenAI (especially local models)

In Steve Sanderson's video he shows using Ollama with the DataGenerator but currently it is up to use to modify the code to do this.

## Other Resources

* [eShopSupport Github](https://github.com/dotnet/eShopSupport)
* [How to add genuinely useful AI to your webapp (not just chatbots) - Steve Sanderson](https://www.youtube.com/watch?v=TSNAvFJoP4M)
* [smartcomponents Github](https://github.com/dotnet/smartcomponents)
* [Semantic Kernel Github](https://github.com/microsoft/semantic-kernel)
* [Onnx Runtime](https://onnxruntime.ai/docs/get-started/with-csharp.html)

If you have a comment, please message me @haleyjason on twitter/X.