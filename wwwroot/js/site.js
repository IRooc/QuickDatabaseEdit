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
            document.location.href = removeQsValues(document.location.href) + '&sortcolumn=' + colName + '&sortdirection=' + dir;
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
let storedItems = [];


const selectItem = (e) => {
    const connectionstring = e.target.dataset.storedvalue;
    if (e.ctrlKey) {
        const ix = storedItems.indexOf(connectionstring);
        if (ix >= 0) {
            storedItems.splice(ix, 1);
            localStorage.setItem('storedItems', JSON.stringify(storedItems));
        }
        e.target.remove();
    } else {
        document.querySelector('input[name="connectionstring"]').value = connectionstring;
        gotoEditButton.click();
    }
}

if (storedItemsDiv) {
    storedItems = JSON.parse(localStorage.getItem('storedItems') ?? "[]");
    storedItems.sort();
    for (let i = 0; i < storedItems.length; i++) {
        const el = document.createElement('button');
        el.type = 'button';
        el.title = 'Ctrl click to remove';
        el.onclick = selectItem;
        el.dataset.storedvalue = storedItems[i];
        el.innerHTML = storedItems[i].replace(/;/g,'<br/>');;
        storedItemsDiv.appendChild(el);
    }
}
if (gotoEditButton) {
    gotoEditButton.addEventListener('click', () => {
        const newUrl = document.querySelector('input[name="connectionstring"]').value;
        console.log('AAA', newUrl, storedItems);
        if (storedItems.indexOf(newUrl) === -1) {
            console.log('BBB', newUrl, storedItems);
            storedItems.push(newUrl);
            localStorage.setItem('storedItems', JSON.stringify(storedItems));
        }
    })
}