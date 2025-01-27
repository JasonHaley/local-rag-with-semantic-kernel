This is the fifth part of the [eShopSupport Series](/2024/08/23/introducing-eshopsupport-series/) which covers the details of the [eShopSupport](https://github.com/dotnet/eshopsupport) GitHub repository.

# PythonInference Project

The [PythonInference](https://github.com/dotnet/eShopSupport/tree/main/src/PythonInference) Project is a python project that provides a web API to classify the case type when a new customer support ticket is entered in the system. It does this by passing the user's comment to the API, which uses a local model from Hugging Face ([cross-encoder/nli-MiniLM2-L6-H768](https://huggingface.co/cross-encoder/nli-MiniLM2-L6-H768)) to classify the text.

![Case Type Dropdown](/img/2024-10-12_img1.jpg)

The PythonInference project is located under the src folder:

![Project Folder](/img/2024-10-12_img2.jpg)

In this entry I'll cover the details of how the PythonInference project works, a few things I found interesting and some thoughts on improvements.

## What does it do?

Steve Sanderson mentions the Python project about 39 minutes into his NDC talk ["How to add genuinely useful AI to your webapp (not just chatbots)"](https://www.youtube.com/watch?v=TSNAvFJoP4M&t=2375s) with the subheading **Small models: HuggingFace/Python/Aspire**:

![Video Screenshot](/img/2024-10-12_img3.jpg)

He mentions there are hundreds of thousands of models on [Hugging Face](https://huggingface.co/models) that are small and provide specific functionality such as classification, toxicity detection, translations, rephrasing, sentiment analysis, etc. As of 10/12/2024. there are now 1,047,898 models on their site directly. If you use Azure and the Azure AI Studio, there is a smaller curated list of 1,595 models from Hugging Face (out of a total of 1,791 models).

### What is Hugging Face?

[Hugging Face](https://huggingface.co/) is a open source community that provides tools and resources for AI and ML. One of its most popular tool libraries is [transformers](https://github.com/huggingface/transformers) which allows you to run some models locally in python (there is also a [javascript version](https://huggingface.co/docs/transformers.js/index) that uses the [ONNX Runtime](https://onnxruntime.ai/)).

As Steve Sanderson points out in his video, most of the sample code for these models is written in python. However if you have a code base that is mostly .NET adding Python to the mix may concern you. This eShopSupport solution shows how a Python project can work fine with Visual Studio and Aspire. If you are curious to learn more, Microsoft has a site about how to [Develop Python applications](https://visualstudio.microsoft.com/vs/features/python/) on the Visual Studio site. There is also a [Python Tools for Visual Studio Github](https://github.com/Microsoft/PTVS/). In my experience I've used Visual Studio Code more often for Python development (which works great too). 

### main.py

The Python project is a Web API (provided by the [FastAPI](https://fastapi.tiangolo.com/) package) and hosted using [Unvicorn](https://www.uvicorn.org/) in the Aspire app model.

The [main.py](https://github.com/dotnet/eShopSupport/blob/main/src/PythonInference/main.py) creates a initializes the FastAPI application and registers two routers that will handle the API endpoint routes (only the classifier is used by the eShopSupport project).

```Python
from fastapi import FastAPI
from routers import classifier, embedder

app = FastAPI()
app.include_router(classifier.router)
app.include_router(embedder.router)
```

### classifier.py

[classifier.py](https://github.com/dotnet/eShopSupport/blob/main/src/PythonInference/routers/classifier.py) defines the API endpoint `/classify`, that accepts a `ClassifyRequest` payload that contains text to classify and a list of candidate labels. When a request is made to this endpoint, it uses a zero-shot classification model to determine which label best matches the text, then returns the most relevant label.

```Python
from fastapi import APIRouter
from pydantic import BaseModel
from transformers import pipeline

router = APIRouter()
classifier = pipeline('zero-shot-classification', model='cross-encoder/nli-MiniLM2-L6-H768', device='cuda')
classifier('warm up', ['a', 'b', 'c'])

class ClassifyRequest(BaseModel):
    text: str
    candidate_labels: list[str]

@router.post("/classify")
def classify_text(item: ClassifyRequest) -> str:
    result = classifier(item.text, item.candidate_labels)
    return result['labels'][0]
```

The `classifier = pipeline('zero-shot-classification', model='cross-encoder/nli-MiniLM2-L6-H768', device='cuda')` line is the initialization of the local model. The line `result = classifier(item.text, item.candidate_labels)` calls the model to get the result. You can look at the [Zero-Shot Classification](https://huggingface.co/cross-encoder/nli-MiniLM2-L6-H768#zero-shot-classification) sample for more information.

> NOTE: By default it is set to use a GPU. If you do have a GPU, there are a few changes you will need to make to the eShopSupport solution. Look at this issue for more detail: [Add instructions for using this app on a system with no GPU ](https://github.com/dotnet/eShopSupport/issues/19)

### embedder.py

[embedder.py](https://github.com/dotnet/eShopSupport/blob/main/src/PythonInference/routers/embedder.py) defines an endpoint `/embed` that accepts an `EmbedRequest` payload that contains a list of sentences to convert to embeddings.

```Python
from fastapi import APIRouter
from pydantic import BaseModel
from sentence_transformers import SentenceTransformer

router = APIRouter()
model = SentenceTransformer('sentence-transformers/all-MiniLM-L6-v2')

class EmbedRequest(BaseModel):
    sentences: list[str]

@router.post("/embed")
def embed_sentences(req: EmbedRequest) -> list[list[float]]:
    embeddings = model.encode(req.sentences)
    return embeddings.tolist()
```

The `model = SentenceTransformer('sentence-transformers/all-MiniLM-L6-v2')` line is the initialization of the local model. The line `embeddings = model.encode(req.sentences)` calls the model to get the embeddings. You can take a look at the [Usage (Sentence-Transformers)](sentence-transformers/all-MiniLM-L6-v2) example on Hugging Face for more information.

> NOTE: As mentioned earlier, /embed is not currently used in the eShopSupport solution.

## Dependencies

The [GitHub Project's Readme](https://github.com/dotnet/eShopSupport/blob/main/README.md) file explains the python requirements:

* [Install Python 3.12.5](https://www.python.org/downloads/release/python-3125/)
* Add the `Python Development` workload to your Visual Studio 2022 if you don't already have it installed.
* From the terminal, at the root of the cloned repo, run:
```Python
pip install -r src/PythonInference/requirements.txt
```
* It also mentions, if the above command doesn't work on Windows, use the following command:
```Python
py -m pip install -r src/PythonInference/requirements.txt
```

If you look at the [requiremnts.txt](https://github.com/dotnet/eShopSupport/blob/main/src/PythonInference/requirements.txt), you'll notice there are quite a few packages required:
```text
annotated-types==0.7.0
anyio==4.4.0
certifi==2024.2.2
charset-normalizer==3.3.2
click==8.1.7
colorama==0.4.6
dnspython==2.6.1
email_validator==2.1.1
exceptiongroup==1.2.1
fastapi==0.111.0
fastapi-cli==0.0.4
filelock==3.14.0
fsspec==2024.5.0
h11==0.14.0
httpcore==1.0.5
httptools==0.6.1
httpx==0.27.0
huggingface-hub==0.23.2
idna==3.7
intel-openmp==2021.4.0
Jinja2==3.1.4
joblib==1.4.2
markdown-it-py==3.0.0
MarkupSafe==2.1.5
mdurl==0.1.2
mkl==2021.4.0
mpmath==1.3.0
networkx==3.3
numpy==1.26.4
orjson==3.10.3
packaging==24.0
pillow==10.3.0
pydantic==2.7.2
pydantic_core==2.18.3
Pygments==2.18.0
python-dotenv==1.0.1
python-multipart==0.0.9
PyYAML==6.0.1
regex==2024.5.15
requests==2.32.3
rich==13.7.1
safetensors==0.4.3
scikit-learn==1.5.0
scipy==1.13.1
sentence-transformers==3.0.0
shellingham==1.5.4
sniffio==1.3.1
starlette==0.37.2
sympy==1.12.1
tbb==2021.12.0
threadpoolctl==3.5.0
tokenizers==0.19.1
--extra-index-url https://download.pytorch.org/whl/cu118
torch==2.3.0+cu118
torchaudio==2.3.0+cu118
torchvision==0.18.0+cu118
tqdm==4.66.4
transformers==4.41.1
typer==0.12.3
typing_extensions==4.12.0
ujson==5.10.0
urllib3==2.2.1
uvicorn==0.30.0
watchfiles==0.22.0
websockets==12.0
```

## How to set it up

Once you have the dependencies above all configured, the PythonInfernce project is loaded by the Aspire AppHost.

```C#
var pythonInference = builder.AddPythonUvicornApp("python-inference",
    Path.Combine("..", "PythonInference"), port: 62394);
```

In the [last blog entry](/2024/10/04/eshopsupport-aspire-projects/), I mentioned the custom extension methods used to add the Python project as an ExecutableResource to the Aspire app model. This means when you start the AppHost, the project is one of the resources:

![Aspire Dashboard](/img/2024-10-12_img4.jpg)

## Points of Interest

These are some points in the code base that I found interesting and will be revisiting when writing my own code. **These things** are the reason I do these code review blog posts.

### Aspire usage

It is nice to see this Python project work with the rest of the Aspire resources in the eShopSupport solution. I think it provides a good example of how to integrate a Python application into a mostly .NET application.

### Simple example using FastAPI

The Python application is good example of a simple web API implementation using FastAPI. This will be a good example for starting a small API and build from in the future. 

## Thoughts on Improvements

This section is more or less to keep the same outline as I've used for the other eShopSupport blog entries - there is not much to improve on with these projects.

### Usage of a virtual environment would be nice

Just like with any technology, when you have to install on the machine it is always nice to use some sort of virtual environment so versions don't collide with other installs on the machine. Like using [nvm](https://github.com/coreybutler/nvm-windows) for multiple versions of node.js on my machine, I have found using virtual environments with Python to be super helpful.

Visual Studio and Visual Studio Code both will work with Python virtual environments, so it would be nice to have that configured already.

![ConfigureOpenTelemetry](/img/2024-10-04_img6.jpg)

## Other Resources

* [eShopSupport Github](https://github.com/dotnet/eShopSupport)
* [How to add genuinely useful AI to your webapp (not just chatbots)](https://www.youtube.com/watch?v=TSNAvFJoP4M)
* [Develop Python applications](https://visualstudio.microsoft.com/vs/features/python/)
* [Visual Studio Code Python Extension](https://marketplace.visualstudio.com/items?itemName=ms-python.python)
* [Hugging Face](https://huggingface.co/)

If you have a comment, please message me @haleyjason on twitter/X.