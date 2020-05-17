window.annotation_browser = function(data, navigation, content, search) {


    const debounce = function(func, wait) {
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

    const cel = function(name, text) {
        const ret = document.createElement(name);

        if (text)
            ret.innerHTML = text;

        return ret;
    }

    const make_depth = function(d) {
        return '&nbsp;'.repeat(d);
    }

    let types = {};

    const navigationContent = cel('ul');

    for (const assembly in data) {
        const li = cel('li', '🎁' + assembly);
        navigationContent.appendChild(li);

        const append_anchor = function(text, link) {
            const li = cel('li');
            const clickable = cel('a', text);
            clickable.href = link;
            li.appendChild(clickable);
            navigationContent.appendChild(li);
        }

        const append_namespace = function(namespace, depth, prepend) {

            for (const key in namespace.Namespaces) {
                const subns = namespace.Namespaces[key];

                const fullName = prepend + '.' + subns.NamespaceName;
                let ndepth = depth;

                if (subns.Types.length > 0 || subns.Comment.length > 0) {

                    const anchor = '#!/' + assembly + '/' + fullName;
                    append_anchor('📂' + make_depth(depth) + '&nbsp;' + fullName, anchor);

                    types[anchor] = {
                        strings: data[assembly].Strings,
                        comment: subns.Comment
                    };


                    subns.Types.forEach(element => {
                        const elementName = fullName + '.' + element.Name;
                        const anchor = '#!/' + assembly + '/' + elementName;
                        append_anchor('📦' + make_depth(depth + 1) + '&nbsp;' + elementName, anchor);

                        types[anchor] = {
                            strings: data[assembly].Strings,
                            comment: element.Comment
                        };
                    });

                    ++ndepth;
                }

                append_namespace(subns, ndepth, fullName);
            }
        }

        append_namespace(data[assembly].Namespaces, 0, '');
    }

    search.addEventListener('input', debounce(function(event) {
        navigation.innerText = '';
        const searchFor = search.value.trim().toLowerCase();
        const filteredNavigationContent = navigationContent.cloneNode(true);
        if (searchFor) {
            for (let i = filteredNavigationContent.childNodes.length - 1; i >= 0; i--) {
                const li = filteredNavigationContent.childNodes[i];
                const liText = li.innerText.toLowerCase();

                if (liText.indexOf(searchFor) === -1) {
                    filteredNavigationContent.removeChild(li);
                }
            }
        }

        navigation.appendChild(filteredNavigationContent);
    }, 250));

    window.addEventListener('popstate', function(event) {
        const comments = types[document.location.hash];
        if (comments) {

            content.innerText = '';

            const c = cel('div');
            comments.comment.forEach(comment => {
                c.appendChild(cel('p', comments.strings[comment]));
            });

            content.appendChild(c);
        }
    });

    navigation.appendChild(navigationContent.cloneNode(true));

};