window.annotation_browser = function(data, navigation, content, search) {

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


    for (const assembly in data) {
        const li = cel('li', '🎁' + assembly);
        navigation.appendChild(li);

        const append_anchor = function(text, link) {
            const li = cel('li');
            const clickable = cel('a', text);
            clickable.href = link;
            li.appendChild(clickable);
            navigation.appendChild(li);
        }

        const append_namespace = function(namespace, depth, prepend) {

            for (const key in namespace.Namespaces) {
                const subns = namespace.Namespaces[key];

                const fullName = prepend + '.' + subns.NamespaceName;
                let ndepth = depth;

                if (subns.Types.length > 0 || subns.Comment.length > 0) {

                    const anchor = '#!/' + assembly + '/' + fullName;
                    append_anchor('📦' + make_depth(depth) + ' ' + fullName, anchor);

                    types[anchor] = {
                        strings: data[assembly].Strings,
                        comment: subns.Comment
                    };


                    subns.Types.forEach(element => {
                        const elementName = fullName + '.' + element.Name;
                        const anchor = '#!/' + assembly + '/' + elementName;
                        append_anchor('#️⃣' + make_depth(depth + 1) + ' ' + elementName, anchor);

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

};