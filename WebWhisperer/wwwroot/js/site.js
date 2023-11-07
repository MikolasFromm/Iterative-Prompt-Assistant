// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification

// get the field of the output element
const queryBuilder = document.getElementById("outputField");
let querySoFar = queryBuilder.value;
let querySoFarLength = 0;
let dotIndex = -1; // Define dotIndex at a higher scope

let nextMoves = [];


// Get references to the file input and upload button
const csvFileInput = document.getElementById("csvFileInput");

async function uploadNewUserQuery() {
    // Get the user input
    const userInput = document.getElementById("inputField").value;

    // get the last character
    const lastChar = userInput.slice(-1);

    // when user input ends with ".", send it to the server
    if (lastChar === ".") {
        // Send the user input to the server
        await fetch('/api/whisper/upload', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(userInput)
        });

        whisperNextMove();

        // Get the first possible whisper from the server
    }
}

async function whisperNextMove() {
    // get the dropdown element
    const dropdown = document.getElementById("dropdown");
    // Clear previous dropdown options
    dropdown.innerHTML = "";

    // get the query build so far
    querySoFar = queryBuilder.value;
    // get the last character
    const lastChar = querySoFar.slice(-1);
    // Calculate the position of the last dot character
    dotIndex = querySoFar.lastIndexOf(".");
    // Get the prefix after the last "."
    const prefix = querySoFar.slice(dotIndex + 1);
  

    // if lastChar is "." or the default empty, whisped next move.
    // also whisper only when the query is longer than before -> meaning that the user is writing, not jsut deleting
    if ((lastChar === "." && querySoFar.length > querySoFarLength) || querySoFar === "") {
        // send request to build all next moves
        const response = await fetch('/api/whisper/process', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(querySoFar)
        });

        nextMoves = await response.json();

        // Call the function with your CSV data
        fetchAndDisplayTable()
    }

    querySoFarLength = querySoFar.length;

    // Filter the available next moves based on the prefix
    const filteredMoves = nextMoves.filter(move => move.startsWith(prefix));

    if (filteredMoves.length > 0) {

        // Calculate the position to place the dropdown
        const inputRect = queryBuilder.getBoundingClientRect();
        const inputLeft = getTextWidth(querySoFar.slice(0, dotIndex), queryBuilder);
        const inputTop = inputRect.bottom;

        // Position the dropdown element
        dropdown.style.display = "block";
        dropdown.style.left = `${inputLeft}px`;

        // create dropdown element for each next move
        dropdown.style.display = "block";
        filteredMoves.forEach((move, index) => {
            const option = document.createElement("div");
            option.innerText = move;

            option.style.cursor = "pointer";
            option.style.padding = "5px"; // extra padding

            // Set background color for the first item
            if (index === 0) {
                option.classList.add("ai-suggestion");
            }

            option.addEventListener("click", () => {
                const prefix = querySoFar.slice(0, dotIndex + 1); // Prefix before the "."
                queryBuilder.value = prefix + move; // Replace existing text after "."
                dropdown.style.display = "none";
            });

            dropdown.appendChild(option);
        });
    }
    // remove dropdown menu when writing text
    else {
        dropdown.style.display = "none";
    }
}

// Function to fetch and display the current table data
async function fetchAndDisplayTable() {
    try
    {
        const response = await fetch('/api/whisper/getCurrent', {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json'
            }
        });

        if (response.ok) {
            // Assuming the response is plain text (CSV data)
            const csvData = await response.text();

            // Call a function to display the CSV data in your table
            populateCsvTable(csvData);
        }
        else
        {
            clearTable();
        }
    }
    catch (error)
    {
        console.error('An error occurred:', error);
    }
}

// Function to send the selected CSV file to the server for processing
async function sendCsvFileToServer(file) {
    try {
        const formData = new FormData();
        formData.append("csvFile", file);

        const response = await fetch('/api/whisper/uploadCsv', {
            method: 'POST',
            body: formData,
        });

        if (response.ok) {
            console.log('CSV file uploaded successfully.');
            // Handle the server's response if needed
        } else {
            console.error('Failed to upload CSV file:', response.statusText);
            // Handle the error if needed
        }
    } catch (error) {
        console.error('An error occurred:', error);
        // Handle the error if needed
    }
}

// Function to parse CSV and populate the table
function populateCsvTable(csvData) {
    const table = document.getElementById("csvTable");
    const tbody = table.querySelector("tbody");
    const thead = table.querySelector("thead");

    // Clear existing table data
    tbody.innerHTML = '';
    thead.innerHTML = '';

    // Split CSV data into rows
    const rows = csvData.split("\n");

    // Create table headers
    const headers = rows[0].split(",");

    // Create a separate table header for each column
    const headerRow = document.createElement("tr");
    headers.forEach((headerText) => {
        const th = document.createElement("th");
        th.textContent = headerText;
        headerRow.appendChild(th);
        thead.appendChild(headerRow);
    });

    // Create table rows and cells
    for (let i = 1; i < rows.length; i++) {
        const rowData = rows[i].split(",");
        const row = document.createElement("tr");
        rowData.forEach((cellData) => {
            const cell = document.createElement("td");
            cell.textContent = cellData;
            row.appendChild(cell);
        });
        tbody.appendChild(row);
    }
}

// Function to clear the table
function clearTable() {
    const table = document.getElementById("csvTable");
    const tbody = table.querySelector("tbody");
    const thead = table.querySelector("thead");

    // Clear existing table data
    tbody.innerHTML = '';
    thead.innerHTML = '';
}

// Function to calculate the width of a given text within an element
function getTextWidth(text, element) {
    const canvas = document.createElement("canvas");
    const context = canvas.getContext("2d");
    context.font = window.getComputedStyle(element).font;
    return context.measureText(text).width;
}


// Function to update the visual selection state
function updateSelection() {
    const dropdownOptions = document.querySelectorAll("#dropdown div");
    dropdownOptions.forEach((option, index) => {
        if (index === selectedOptionIndex) {
            option.classList.add("selected");
        } else {
            option.classList.remove("selected");
        }
    });
}


let selectedOptionIndex = -1;

// Add keydown event listener to handle navigation and selection
document.addEventListener("keydown", (event) => {
    const dropdownOptions = document.querySelectorAll("#dropdown div");

    if (event.key === "ArrowDown") {
        // Navigate down
        selectedOptionIndex = (selectedOptionIndex + 1) % dropdownOptions.length;
        updateSelection();
    } else if (event.key === "ArrowUp") {
        // Navigate up
        selectedOptionIndex = (selectedOptionIndex - 1 + dropdownOptions.length) % dropdownOptions.length;
        updateSelection();
    } else if (event.key === "Enter") {
        // Select option
        if (selectedOptionIndex >= 0 && selectedOptionIndex < dropdownOptions.length) {
            const prefix = querySoFar.slice(0, dotIndex + 1); // Prefix before the "."
            const selectedMove = dropdownOptions[selectedOptionIndex].innerText;
            queryBuilder.value = prefix + selectedMove; // Replace existing text after "."
            dropdown.style.display = "none";
        }
    }
});

// Add an event listener to the file input to handle file selection
csvFileInput.addEventListener("change", () => {
    const selectedFile = csvFileInput.files[0];

    if (selectedFile) {
        // You can display the selected file name or handle the file here
        console.log("Selected CSV file: " + selectedFile.name);

        // Trigger the file upload automatically
        sendCsvFileToServer(selectedFile);

        // Reset the file input to allow selecting another file if needed
        //csvFileInput.value = "";
    }
});