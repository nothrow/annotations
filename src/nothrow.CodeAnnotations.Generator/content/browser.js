window.annotation_browser = function(data, divs) {

    const { navigation, content, search, search_caption, generatedOn, version, tag_list } = divs;
    const assemblies = data.Assemblies;

    generatedOn.innerText = data.GeneratedOn;
    version.innerText = data.Version;

    let allTags = {};

    const debounce = (func, wait) => {
        var timeout;
        return function() {
            var context = this,
                args = arguments;
            var later = function() {
                timeout = null;
                func.apply(context, args);
            };
            clearTimeout(timeout);
            timeout = setTimeout(later, wait);
        };
    };

    const cel = (name, text) => {
        const ret = document.createElement(name);

        if (text)
            ret.innerHTML = text;

        return ret;
    };

    const make_depth = function(d) {
        return '&nbsp;'.repeat(d);
    };

    let types = {};

    const navigationContent = cel('ul');

    for (const assembly in assemblies) {
        const li = cel('li', '🎁' + assembly);
        navigationContent.appendChild(li);

        let parentIdStack = [];
        const pushParent = function() {
            parentIdStack.push(navigationContent.childNodes.length - 1);
        };
        const popParent = function() {
            parentIdStack.pop();
        }


        const readTags = (comments) => {
            comments.forEach(comment => {
                assemblies[assembly].Strings[comment].Tags.forEach(tag => {
                    if (tag in allTags)
                        allTags[tag]++;
                    else
                        allTags[tag] = 1;

                });
            });
        }


        pushParent();


        const append_anchor = function(text, link) {
            const li = cel('li');

            li.dataset.parent = parentIdStack[parentIdStack.length - 1];
            li.dataset.refCount = 0;
            li.dataset.anchor = link;

            const clickable = cel('a', text);
            clickable.href = link;
            li.appendChild(clickable);
            navigationContent.appendChild(li);
        }

        const append_namespace = function(namespace, depth, prepend) {

            for (const key in namespace.Namespaces) {

                const subns = namespace.Namespaces[key];

                const fullName = prepend + '.' + subns.NamespaceName;
                let doPop = () => {};
                let ndepth = depth;

                if (subns.Types.length > 0 || subns.Comment.length > 0) {

                    const anchor = '#!/' + assembly + '/' + fullName;
                    append_anchor('📂' + make_depth(depth) + '&nbsp;' + fullName, anchor);

                    pushParent();
                    doPop = popParent;

                    readTags(subns.Comment);

                    types[anchor] = {
                        strings: assemblies[assembly].Strings,
                        comment: subns.Comment
                    };


                    subns.Types.forEach(element => {
                        const elementName = fullName + '.' + element.Name;
                        const anchor = '#!/' + assembly + '/' + elementName;
                        append_anchor('📦' + make_depth(depth + 1) + '&nbsp;' + elementName, anchor);

                        readTags(element.Comment);

                        types[anchor] = {
                            strings: assemblies[assembly].Strings,
                            comment: element.Comment
                        };
                    });

                    ++ndepth;
                }

                append_namespace(subns, ndepth, fullName);

                doPop();
            }
        }

        append_namespace(assemblies[assembly].Namespaces, 0, '');
    }

    const matches = function(needle, haystack, hayProps) {

        if (needle[0] === '#') // search for tag
        {
            if (!hayProps)
                return false;

            for (let comment in hayProps.comment) {
                const tags = hayProps.strings[hayProps.comment[comment]].Tags;
                for (let tag in tags) {
                    if (tags[tag] === needle)
                        return true;
                }
            }

            return false;
        }


        const splitTextByCapsAndDot = function(t) {
            let rv = [];
            let buf = '';
            const flushBuffer = function() {
                if (buf.length > 0)
                    rv.push(buf);

                buf = '';
            };

            for (let i = 0; i < t.length; i++) {
                const ti = t[i];
                if (ti === '.')
                    flushBuffer();
                else {
                    if (ti.toUpperCase() === ti)
                        flushBuffer();

                    buf += ti;
                }
            }
            flushBuffer();
            return rv;
        };
        // behave in 'special' way for dots, and capitals, the same way as resharper does
        // let's Met.Ent match also METadata...ENTity
        // also Met.EntNa should match Metadata.EntityName
        // ported from my code @ swql studio
        if (haystack.toLowerCase().indexOf(needle) !== -1)
            return true;

        var filter = splitTextByCapsAndDot(needle);
        var text = splitTextByCapsAndDot(haystack);

        let textPivot = 0;
        for (let filterPivot = 0; filterPivot < filter.length; filterPivot++) {
            for (; textPivot <= text.length; textPivot++) {
                if (textPivot == text.length)
                    return false;
                if (text[textPivot].toLowerCase().indexOf(filter[filterPivot].toLowerCase()) === 0)
                    break;
            }
            textPivot++;
        }
        return true;

    };

    search.addEventListener('input', debounce(() => {
        navigation.innerText = '';
        const searchFor = search.value.trim();
        const filteredNavigationContent = navigationContent.cloneNode(true);

        const totalNodes = filteredNavigationContent.childNodes.length;
        let visibleNodes = filteredNavigationContent.childNodes.length;

        if (searchFor) {
            for (let i = filteredNavigationContent.childNodes.length - 1; i >= 0; i--) {

                const li = filteredNavigationContent.childNodes[i];
                const liText = li.innerText;

                const childVisible = parseInt(li.dataset.refCount) !== 0;
                const matchesFilter = matches(searchFor, liText, types[li.dataset.anchor]);

                if (!matchesFilter)
                    visibleNodes--;

                if (!childVisible && !matchesFilter) {
                    filteredNavigationContent.removeChild(li);
                } else {

                    if (!matchesFilter) {
                        li.classList.add('visible-child');
                    }
                    if (li.dataset.parent) {
                        const liParent = filteredNavigationContent.childNodes[li.dataset.parent];
                        liParent.dataset.refCount = parseInt(liParent.dataset.refCount) + 1;
                    }
                }
            }
        }

        search_caption.innerText = "Filter matches " + visibleNodes + "/" + totalNodes;

        navigation.appendChild(filteredNavigationContent);
    }, 250));

    const onPopState = function() {
        const comments = types[document.location.hash];
        if (comments) {

            content.innerText = '';

            const c = cel('div');
            comments.comment.forEach(comment => {
                c.appendChild(cel('p', comments.strings[comment].String));
            });

            content.appendChild(c);
        }
    };

    window.addEventListener('popstate', onPopState);

    onPopState();

    navigation.appendChild(navigationContent.cloneNode(true));



    (function() {
        const tagUl = cel('ul');

        for (const tag in allTags) {
            const li = cel('li');
            const a = cel('a', tag + '(' + allTags[tag] + ')');
            a.href = '#';

            a.addEventListener('click', function() {
                search.value = tag;

                search.dispatchEvent(new Event('input', {
                    bubbles: true,
                    cancelable: true,
                }));

            });

            li.appendChild(a);
            tagUl.appendChild(li);
        }

        tag_list.appendChild(tagUl);
    })();




};