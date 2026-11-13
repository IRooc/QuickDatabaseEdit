var currentQuerystring = removeQsValues(document.location.search);
function searchTable(event) {
    var search = event.target.value.toLowerCase();
    localStorage.setItem('search' + currentQuerystring, search);
    var el = document.getElementById('tablerows');
    var rows = el.querySelectorAll('tr.clickable');
    if (search == '') {
        var hidden = el.querySelectorAll('tr.clickable.hide')
        for (let i = 0; i < hidden.length; i++) {
            var row = hidden[i];
            row.classList.remove('hide');
        }
    } else {
        for (let i = 0; i < rows.length; i++) {
            var row = rows[i];
            if (row.textContent.toLowerCase().indexOf(search) >= 0) {
                row.classList.remove('hide');
            } else {
                row.classList.add('hide');
            }
        }
    }
}

//trigger search event
var curSearch = localStorage.getItem('search' + currentQuerystring);
var search = document.getElementById('search');
if (curSearch && search) {
    search.value = curSearch;
    var event = new Event('input', {
        bubbles: true,
        cancelable: true,
    });
    search.dispatchEvent(event);
}

//add sort column clicks
var sorters = document.querySelectorAll('th.sort');
for (let i = 0; i < sorters.length; i++) {
    var sort = sorters[i];
    sort.addEventListener('click', (e) => {
        document.body.style.cursor = "wait";
        var colName = e.target.innerText;
        var dir = 'asc'
        if (document.location.href.indexOf('sortdirection=asc') > 0) dir = 'desc';
        setTimeout(() => {
            document.location.href = removeQsValues(document.location.href) + '&sortcolumn=' + encodeURIComponent(colName) + '&sortdirection=' + dir;
        }, 100)
    })
}

var msgButton = document.querySelector('.message button');
if (msgButton) {
    msgButton.addEventListener("click", (e) => { e.target.parentElement.remove(); });
}

async function newguid(event) {
    const response = await fetch('?handler=Guid');
    event.target.parentElement.querySelector('input').value = (await response.text()).substring(1, 37);
}

function removeQsValues(url) {
    return url.replace(/&sortcolumn=[^&]+/gi, '').replace(/&sortdirection=[^&]+/gi, '');
}

function copyCurrentForm() {
    const elements = Array.from(document.querySelectorAll('input,select,textarea'));
    const shownElements = elements.filter(el => el.type !== 'hidden');
    const mapped = shownElements.map(function(el, ix) { return{ key: el.name, val: el.value } });
    localStorage.setItem('form', JSON.stringify(mapped));
    console.log(elements, shownElements, mapped, localStorage.getItem('form'));
}

function pasteCurrentForm() {
    const stored = localStorage.getItem('form');
    const elements = JSON.parse(stored);
    for (let i = 0; i < elements.length; i++) {
        var el = elements[i]
        document.querySelector('[name="' + el.key + '"]').value = el.val;
    }
}



//remember previous items
const gotoEditButton = document.getElementById('gotoEdit');
const storedItemsDiv = document.getElementById('storedItemsDiv');
const databaseTypeSelect = document.getElementById('databaseType');
const connectionstringInput = document.getElementById('connectionstring');
let storedItems = [];


const selectItem = (e) => {
    const connectionstring = e.currentTarget.dataset.storedvalue;
    const databaseType = e.currentTarget.dataset.databaseType;
    if (e.ctrlKey) {
        const ix = storedItems.findIndex(item => item.connectionstring === connectionstring && item.databaseType === databaseType);
        if (ix >= 0) {
            storedItems.splice(ix, 1);
            localStorage.setItem('storedItems', JSON.stringify(storedItems));
        }
        e.currentTarget.remove();
    } else {
        connectionstringInput.value = connectionstring;
        databaseTypeSelect.value = databaseType;
        updateConnectionstringHint();
        gotoEditButton.click();
    }
}

if (storedItemsDiv) {
    // Old saved connections were plain strings and always used MSSQL.
    storedItems = JSON.parse(localStorage.getItem('storedItems') ?? "[]")
        .map(item => typeof item === 'string' ? { databaseType: 'MSSQL', connectionstring: item } : item);
    storedItems.sort((a, b) => (a.databaseType + a.connectionstring).localeCompare(b.databaseType + b.connectionstring));
    for (let i = 0; i < storedItems.length; i++) {
        const el = document.createElement('button');
        el.type = 'button';
        el.title = 'Ctrl click to remove';
        el.onclick = selectItem;
        el.dataset.storedvalue = storedItems[i].connectionstring;
        el.dataset.databaseType = storedItems[i].databaseType;
        el.textContent = storedItems[i].databaseType + '\n' + storedItems[i].connectionstring.replace(/;/g, '\n');
        storedItemsDiv.appendChild(el);
    }
}
if (gotoEditButton) {
    gotoEditButton.form.addEventListener('submit', () => {
        const connectionstring = connectionstringInput.value;
        const databaseType = databaseTypeSelect.value;
        if (!storedItems.some(item => item.connectionstring === connectionstring && item.databaseType === databaseType)) {
            storedItems.push({ databaseType, connectionstring });
            localStorage.setItem('storedItems', JSON.stringify(storedItems));
        }
    })
}

function updateConnectionstringHint() {
    const example = databaseTypeSelect.value === 'Postgres'
        ? 'Host=localhost;Port=5432;Database=mydatabase;Username=postgres;Password=yourpassword'
        : 'Server=localhost;Database=mydatabase;Integrated Security=True;TrustServerCertificate=True';
    connectionstringInput.placeholder = example;
    document.getElementById('connectionstringHint').textContent = 'Example: ' + example;
}

if (databaseTypeSelect) {
    databaseTypeSelect.addEventListener('change', updateConnectionstringHint);
    updateConnectionstringHint();
}
